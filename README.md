# TaskFlow

A small but complete project/task-management platform (think a minimal Jira), built as an interview-prep MVP to demonstrate the stack in a specific Senior Software Engineer job posting: **.NET Core + React, Azure PaaS, SQL + NoSQL, REST + GraphQL, CI/CD, event-driven microservices, and design patterns** — end to end, running, with tests, not just a diagram.

- [What this demonstrates, mapped to the job posting](#what-this-demonstrates-mapped-to-the-job-posting)
- [Architecture](#architecture)
- [Running it locally](#running-it-locally)
- [Running the tests](#running-the-tests)
- [Repository layout](#repository-layout)
- [Design decisions and patterns](#design-decisions-and-patterns)
- [From docker-compose to Azure](#from-docker-compose-to-azure)
- [Real bugs found while building this](#real-bugs-found-while-building-this)
- [What's intentionally out of scope for an MVP](#whats-intentionally-out-of-scope-for-an-mvp)

## What this demonstrates, mapped to the job posting

| Job requirement | Where it lives here |
|---|---|
| Deep proficiency in .NET Core backend | `backend/src` — .NET 8, ASP.NET Core Web API, Clean Architecture (Domain/Application/Infrastructure/Api) |
| Deep proficiency in React | `frontend/taskflow-web` — React 19 + TypeScript, hooks, React Query, React Router |
| Modern web tech: HTML/CSS/JS/frameworks | `frontend/taskflow-web/src` — Vite, TS, CSS, no legacy tooling |
| Azure PaaS / cloud-native design | `infra/bicep` — App Service, Azure SQL, Cosmos DB (Mongo API), Service Bus, Static Web Apps, Key Vault, App Insights |
| SQL **and** NoSQL database experience | Azure SQL / SQL Server via EF Core (`TaskFlow.Infrastructure/Persistence`) for the relational domain; MongoDB (`TaskFlow.Notifications`) for the notification/activity-feed side |
| REST **and** GraphQL API development | REST controllers for commands (`TaskFlow.Api/Controllers`); HotChocolate GraphQL for reads (`TaskFlow.Api/GraphQL/Query.cs`) |
| CI/CD pipelines & DevOps practices | `.github/workflows` — backend tests, frontend tests, multi-service Docker build/push |
| Problem-solving / attention to detail | [Real bugs found while building this](#real-bugs-found-while-building-this) — three real defects found and fixed via actual reproduction, not just code review |
| *(Nice-to-have)* Event-driven & microservices architecture | Two independently deployable services (`TaskFlow.Api`, `TaskFlow.Notifications`) communicating only through RabbitMQ domain events — see [Architecture](#architecture) |
| *(Nice-to-have)* Software design patterns | CQRS via MediatR, Repository + Unit of Work, pipeline/decorator behaviors, a swappable `IEventBus` port — see [Design decisions and patterns](#design-decisions-and-patterns) |
| *(Nice-to-have)* Cloud-native technology solutions | 12-factor config (env vars, no hardcoded hosts), containerized services, IaC for the Azure target topology |
| *(Nice-to-have)* Web security best practices | JWT bearer auth, BCrypt password hashing, centralized exception-to-HTTP-status mapping, CORS allow-list, parameterized queries throughout (EF Core), secrets via Key Vault + managed identity in the IaC design |

## Architecture

```
                    ┌─────────────────────┐
                    │   taskflow-web       │  React + TS (Vite)
                    │   (nginx container)  │  REST for writes, GraphQL for reads
                    └──────────┬───────────┘
                               │
              ┌────────────────┼─────────────────────┐
              │                │                      │
              ▼                ▼                      ▼
      ┌───────────────┐                        ┌──────────────────────┐
      │  TaskFlow.Api  │                        │ TaskFlow.Notifications│
      │ REST + GraphQL │                        │   REST (read-only)    │
      │  ASP.NET Core  │                        │   ASP.NET Core        │
      └───────┬────────┘                        └───────────┬───────────┘
              │                                              │
              │ EF Core                                      │ MongoDB.Driver
              ▼                                              ▼
      ┌───────────────┐                             ┌─────────────────┐
      │  SQL Server /  │                             │  MongoDB /       │
      │  Azure SQL     │                             │  Cosmos DB (Mongo)│
      └───────────────┘                             └─────────────────┘
              │
              │ publishes domain events (TaskAssigned, TaskStatusChanged)
              ▼
      ┌───────────────────────────┐
      │  RabbitMQ / Azure Service │──── consumed by ───▶ TaskFlow.Notifications
      │  Bus (topic exchange)     │
      └───────────────────────────┘
```

**Why two services instead of one.** `TaskFlow.Api` owns the relational domain (users, projects, tasks) and never talks to Mongo. `TaskFlow.Notifications` owns notifications and never talks to SQL Server. They don't share code, a database, or a deployment — the *only* coupling between them is the shape of the domain events on the message bus. That's what makes this "microservices" rather than "one app split into two folders": either one can be redeployed, rescaled, or rewritten in a different stack without touching the other, as long as the event contract holds.

**Why REST for writes and GraphQL for reads.** Commands (`POST /api/projects`, `PATCH /api/tasks/{id}/status`, …) map naturally onto HTTP verbs and status codes, and don't benefit from GraphQL's flexible shaping — a command either succeeds or it doesn't. Reads are where GraphQL earns its place: `ProjectDetailPage` needs a project **and** its tasks in one round trip, so the client asks for exactly that shape once instead of the frontend orchestrating two REST calls or the backend growing a bespoke "get project with tasks" endpoint for every screen that needs a slightly different shape.

## Running it locally

Requires Docker only (no local .NET SDK or Node install needed — the SDKs run inside the build containers).

```bash
docker compose up -d --build
```

This starts SQL Server, MongoDB, RabbitMQ, both backend services, and the frontend. On first boot, `TaskFlow.Api` applies EF Core migrations automatically.

| Service | URL |
|---|---|
| Frontend | http://localhost:5173 |
| API (REST + GraphQL + Swagger) | http://localhost:5080 (Swagger at `/swagger`, GraphQL at `/graphql`) |
| Notifications API | http://localhost:5081 |
| RabbitMQ management UI | http://localhost:15672 (guest/guest) |

Register two users from the UI (or via `POST /api/auth/register`), create a project with one, add the other as a member, create a task, assign it, and move it across the board — the assignee should see a notification appear (polled every 15s) within a few seconds, having round-tripped through RabbitMQ into MongoDB and back out over REST.

To stop and remove all data: `docker compose down -v`.

## Running the tests

```bash
# Backend — unit tests (domain + command/query handlers) and integration tests
# (full ASP.NET Core pipeline against an in-memory EF Core provider)
cd backend
dotnet test tests/TaskFlow.Application.Tests
dotnet test tests/TaskFlow.Api.IntegrationTests

# Frontend — component tests (Vitest + React Testing Library)
cd frontend/taskflow-web
npm test
```

Both back-end test projects and the CI workflows run without any external dependency (SQL Server, RabbitMQ) — the integration tests swap in EF Core's InMemory provider and a fake in-process event bus (see `CustomWebApplicationFactory`), so `dotnet test` is self-contained.

## Repository layout

```
backend/
  src/
    TaskFlow.Domain/          entities, enums, domain events — no framework dependencies
    TaskFlow.Application/     CQRS commands/queries (MediatR), validators, DTOs, ports (interfaces)
    TaskFlow.Infrastructure/  EF Core, repositories, RabbitMQ publisher, JWT, password hashing
    TaskFlow.Api/             REST controllers, GraphQL schema, auth, exception middleware
    TaskFlow.Notifications/   independent microservice: RabbitMQ consumer -> MongoDB -> REST
  tests/
    TaskFlow.Application.Tests/       unit tests (domain rules + handlers, mocked ports)
    TaskFlow.Api.IntegrationTests/    WebApplicationFactory tests against the real HTTP pipeline
frontend/
  taskflow-web/               React + TypeScript (Vite), React Query, React Router
infra/
  bicep/                      Azure PaaS topology (design-only, not deployed — see below)
.github/workflows/            CI: backend tests, frontend tests, Docker build/push
docker-compose.yml            full local stack
```

## Design decisions and patterns

- **Clean Architecture / Onion layering.** `Domain` has zero package references. `Application` depends only on `Domain` plus MediatR/FluentValidation abstractions — it never references EF Core's `SqlServer` package, ASP.NET Core, or RabbitMQ directly, only the interfaces in `Application/Common/Interfaces`. `Infrastructure` and `Api` are where those interfaces get real implementations. This is what makes the integration tests able to swap SQL Server for an in-memory provider and RabbitMQ for a fake bus without touching a single command handler.
- **CQRS via MediatR**, not for its own sake but because it gives every command/query a single-responsibility handler and a place to hang cross-cutting behavior: `ValidationBehavior` runs FluentValidation before any handler executes, and `LoggingBehavior` times and logs every request — both are decorators registered once in `Application/DependencyInjection.cs`, not copy-pasted into each handler.
- **Repository + Unit of Work**, deliberately explicit rather than "just inject `DbContext` everywhere." `IUnitOfWork.CommitAsync` is the one place that (a) saves changes and (b) publishes any domain events collected during that unit of work — *after* the save succeeds, never before. See `ApplicationDbContext.CommitAsync` and the comment on `AssignTaskCommandHandler` for why that ordering matters (a broker outage or event-bus bug must never look like a successful assignment).
- **Ports and adapters at the message boundary.** `IEventBus` is the only thing `Application` knows about publishing events. `TaskFlow.Infrastructure.Messaging.RabbitMqEventBus` is the local adapter; swapping it for an Azure Service Bus adapter (matching `infra/bicep/modules/serviceBus.bicep`) is the only change needed to move this to Azure — no command handler changes.
- **Domain entities enforce their own invariants.** `TaskItem.ChangeStatus`, `Project.AddMember`, etc. are the only way to mutate those objects — there's no public setter a handler could use to put an entity in an inconsistent state, and the domain events (`TaskAssignedEvent`, `TaskStatusChangedEvent`) are raised from inside the entity, not bolted on by the handler after the fact.

## From docker-compose to Azure

`infra/bicep` is a **design artifact, not a deployed environment** — running `az deployment group create` against it costs money and needs a subscription this MVP doesn't assume one has. It exists to show the intended production topology and prove the local architecture actually maps onto real Azure PaaS services, not just Docker:

| Local (docker-compose) | Azure (infra/bicep) |
|---|---|
| `sqlserver` container | Azure SQL Database, serverless tier (auto-pauses when idle) |
| `mongo` container | Cosmos DB for MongoDB (same wire protocol, same driver, no code change) |
| `rabbitmq` container | Service Bus namespace + topic, with per-event-type subscription filters mirroring the local queue bindings |
| `taskflow-api` / `taskflow-notifications` containers | App Service (Linux, container-based), one plan, two sites |
| `taskflow-web` container (nginx) | Static Web Apps (build-from-repo, no server needed for a pure SPA) |
| secrets in `docker-compose.yml` env vars | Key Vault, referenced by App Service via managed identity — see `modules/keyVault.bicep` |
| container logs | Log Analytics + Application Insights (`modules/monitoring.bicep`) |

`az bicep build --file infra/bicep/main.bicep` compiles clean; the linter's `outputs-should-not-contain-secrets` warnings on the SQL/Cosmos/Service Bus connection strings are intentional and called out in a comment at the top of `main.bicep`, along with what hardening them further would look like.

## Real bugs found while building this

Kept here on purpose — the ability to find and root-cause these matters more than never having written them:

1. **EF Core inserted an UPDATE instead of an INSERT for a new child entity, throwing `DbUpdateConcurrencyException` ("0 rows affected").** Root cause: every entity's `Id` is a GUID assigned client-side (`Guid.NewGuid()` in `BaseEntity`'s property initializer) rather than generated by the database. When a brand-new `ProjectMember` was added purely by mutating `Project`'s tracked collection (no explicit `DbSet.Add()` call), EF Core's change tracker saw a primary key that already had a non-default value and assumed the row already existed in the database — so it emitted an `UPDATE ... WHERE Id = @p` that matched zero rows. Fixed by configuring `.Property(x => x.Id).ValueGeneratedNever()` on every entity, which tells EF Core these keys are always application-assigned and removes the "might already exist" ambiguity. (`backend/src/TaskFlow.Infrastructure/Persistence/Configurations/*.cs`)
2. **Domain events published to RabbitMQ arrived with every field empty except `OccurredOn`.** `RabbitMqEventBus.PublishAsync(IDomainEvent domainEvent, ...)` built an anonymous object with `Payload = domainEvent` — but `domainEvent`'s *compile-time* type is the `IDomainEvent` interface, which only declares `OccurredOn`. `System.Text.Json` serializes based on a property's declared type, not its runtime type, so every other field (`TaskId`, `AssigneeId`, …) was silently dropped — no exception, just missing data. Fixed by explicitly serializing with the runtime type: `JsonSerializer.SerializeToElement(domainEvent, domainEvent.GetType())`. (`backend/src/TaskFlow.Infrastructure/Messaging/RabbitMqEventBus.cs`)
3. **The Notifications consumer then failed to deserialize `TaskStatusChangedEvent` at all**, once bug #2 was fixed and real data started arriving: the domain event carries `TaskState` as a native C# enum, which `System.Text.Json` serializes as a number by default, while the consumer's independent contract copy (`TaskFlow.Notifications.Models.TaskStatusChangedEvent`) declares the same field as a `string` — an intentional decoupling choice (see the comment on `DomainEventContracts.cs`) that broke without a matching converter on the publish side. Fixed by adding `JsonStringEnumConverter` to the publisher's serializer options.

All three were caught by actually running the full stack end-to-end with `docker compose up` and exercising the register → create project → add member → create task → assign → change status flow with `curl`, not by inspection — #1 and #2 in particular produce no compiler warning and no obviously-wrong code on read-through.

## What's intentionally out of scope for an MVP

- No real-time push for notifications (SignalR/WebSockets) — polling every 15s was the deliberate trade-off to avoid wiring a socket layer through two services and the Azure topology for a demo. Noted as the natural next step in `useNotifications.ts`.
- No refresh tokens — the JWT simply expires after 120 minutes and the user logs in again. A production version would add a refresh-token flow.
- No outbox pattern for the event publisher — `RabbitMqEventBus` logs and swallows a broker outage rather than guaranteeing delivery (see the comment in `PublishAsync`). Fine for a demo; a production system would add an outbox table.
- Drag-and-drop on the task board was cut in favor of a plain status `<select>`, to keep the frontend's scope on data flow (REST/GraphQL/auth/state) rather than a drag-and-drop library.
