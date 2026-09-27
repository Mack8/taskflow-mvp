param resourceToken string
param location string
param tags object

// Replaces the local RabbitMQ topic exchange. A topic (not a queue) because
// the domain publishes to "taskflow.events" and lets subscribers filter by
// routing key — Service Bus topics + subscription filter rules are the
// direct analog of RabbitMQ's topic-exchange + queue-binding model used in
// TaskFlow.Infrastructure.Messaging.RabbitMqEventBus.
resource serviceBusNamespace 'Microsoft.ServiceBus/namespaces@2024-01-01' = {
  name: 'sb-${resourceToken}'
  location: location
  tags: tags
  sku: {
    name: 'Standard'
    tier: 'Standard'
  }
}

resource eventsTopic 'Microsoft.ServiceBus/namespaces/topics@2024-01-01' = {
  parent: serviceBusNamespace
  name: 'taskflow-events'
  properties: {
    defaultMessageTimeToLive: 'P14D'
  }
}

resource notificationsSubscription 'Microsoft.ServiceBus/namespaces/topics/subscriptions@2024-01-01' = {
  parent: eventsTopic
  name: 'notifications-service'
  properties: {
    maxDeliveryCount: 10
    deadLetteringOnMessageExpiration: true
  }
}

// Filters mirror the two routing-key bindings NotificationEventConsumer
// declares locally (TaskAssignedEvent, TaskStatusChangedEvent).
resource taskAssignedFilter 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = {
  parent: notificationsSubscription
  name: 'TaskAssignedEvent'
  properties: {
    filterType: 'CorrelationFilter'
    correlationFilter: {
      label: 'TaskAssignedEvent'
    }
  }
}

resource taskStatusChangedFilter 'Microsoft.ServiceBus/namespaces/topics/subscriptions/rules@2024-01-01' = {
  parent: notificationsSubscription
  name: 'TaskStatusChangedEvent'
  properties: {
    filterType: 'CorrelationFilter'
    correlationFilter: {
      label: 'TaskStatusChangedEvent'
    }
  }
}

output connectionString string = listKeys('${serviceBusNamespace.id}/AuthorizationRules/RootManageSharedAccessKey', '2024-01-01').primaryConnectionString
