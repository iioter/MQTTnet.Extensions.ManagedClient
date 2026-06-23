# MQTTnet.Extensions.ManagedClient

[中文文档](README.zh-CN.md)

In industrial IoT, edge gateway, telemetry, and system integration scenarios, an MQTT client is usually not a short-lived connection that publishes once and exits. It is a long-running component that must stay online, keep subscriptions active, and continue publishing application messages even when the network, broker, or device environment is unstable.

Without a managed client, application code often grows its own reconnect loops, connection-state checks, publish queues, subscription recovery logic, and failure handling. `MQTTnet.Extensions.ManagedClient` packages those concerns into a reusable client layer: configure the MQTT connection, subscribe to topics, enqueue outgoing messages, and let the managed client maintain the connection, reconnect after disconnects, restore subscriptions, and process pending messages in order.

This helps .NET developers focus on device data, business logic, and integration workflows instead of repeatedly rebuilding MQTT reliability plumbing.

`MQTTnet.Extensions.ManagedClient` is a managed MQTT client extension for MQTTnet 5.x. It keeps the client connection alive, reconnects automatically, restores subscriptions after reconnect, and publishes queued application messages from a managed queue.

It is especially useful for:

- Industrial gateways, edge nodes, data collectors, and other .NET services that run continuously.
- Telemetry upload scenarios that need recoverable publishing during unstable network conditions.
- Command, telemetry, and state synchronization flows that must restore subscriptions after reconnecting.
- Applications that need consistent events for connection changes, reconnect failures, subscription synchronization, skipped messages, and processed messages.

## Features

- Automatic reconnect with configurable delay.
- Managed publish queue through `EnqueueAsync`.
- Optional pending-message queue limit and overflow strategy.
- Subscription tracking with automatic resubscription after reconnect.
- Event hooks for connection, reconnect failures, subscription synchronization, skipped messages, processed messages, and received messages.
- Optional storage abstraction for queued messages.

## Installation

NuGet:

```xml
<PackageReference Include="IoTGateway.MQTTnet.Extensions.ManagedClient" Version="5.0.0" />
```

## Quick Start

```csharp
using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

var factory = new MqttClientFactory();
using var client = factory.CreateManagedMqttClient();

client.ConnectedAsync += args =>
{
    Console.WriteLine("Connected.");
    return Task.CompletedTask;
};

client.DisconnectedAsync += args =>
{
    Console.WriteLine("Disconnected.");
    return Task.CompletedTask;
};

client.ApplicationMessageReceivedAsync += args =>
{
    var payload = args.ApplicationMessage.ConvertPayloadToString();
    Console.WriteLine($"Received {args.ApplicationMessage.Topic}: {payload}");
    return Task.CompletedTask;
};

var options = new ManagedMqttClientOptionsBuilder()
    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
    .WithMaxPendingMessages(1000)
    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
    .WithClientOptions(builder =>
    {
        builder
            .WithTcpServer("iotgateway.net", 1883)
            .WithCredentials("admin", "iotgateway.net")
            .WithClientId($"managed-client-{Guid.NewGuid():N}")
            .WithCleanSession();
    })
    .Build();

await client.StartAsync(options);
await client.SubscribeAsync("iotgateway/demo/managed-client", MqttQualityOfServiceLevel.AtLeastOnce);
await client.EnqueueAsync("iotgateway/demo/managed-client", "hello from managed client", MqttQualityOfServiceLevel.AtLeastOnce);
```

## Demo

Run the demo project:

```powershell
dotnet run --project src/MQTTnet.Extensions.ManagedClient.Demo/MQTTnet.Extensions.ManagedClient.Demo.csproj
```

The demo connects to the broker, subscribes to the configured topic, publishes messages through the managed queue, prints received messages, and stops cleanly when you press `Ctrl+C`.

## Main API

- `MqttClientFactory.CreateManagedMqttClient()` creates the managed client.
- `ManagedMqttClientOptionsBuilder` builds reconnect, queue, storage, and MQTT client options.
- `IManagedMqttClient.StartAsync(...)` starts connection maintenance.
- `IManagedMqttClient.EnqueueAsync(...)` queues application messages for publishing.
- `IManagedMqttClient.SubscribeAsync(...)` and `UnsubscribeAsync(...)` manage subscriptions.
- `IManagedMqttClient.StopAsync(...)` stops publishing and disconnects.

## License

This project is licensed under the [MIT License](LICENSE).
