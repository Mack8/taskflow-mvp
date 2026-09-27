FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

COPY ["src/TaskFlow.Notifications/TaskFlow.Notifications.csproj", "src/TaskFlow.Notifications/"]
RUN dotnet restore "src/TaskFlow.Notifications/TaskFlow.Notifications.csproj"

COPY src/TaskFlow.Notifications/ src/TaskFlow.Notifications/
RUN dotnet publish "src/TaskFlow.Notifications/TaskFlow.Notifications.csproj" -c Release -o /app/publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS final
WORKDIR /app
EXPOSE 8080
ENV ASPNETCORE_URLS=http://+:8080

COPY --from=build /app/publish .
ENTRYPOINT ["dotnet", "TaskFlow.Notifications.dll"]
