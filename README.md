# MQTTnet.Extensions.ManagedClient

[中文文档](README.zh-CN.md)

`MQTTnet.Extensions.ManagedClient` is a managed MQTT client extension for MQTTnet 8.x. It keeps the client connection alive, reconnects automatically, restores subscriptions after reconnect, and publishes queued application messages from a managed queue.

## Features

- Automatic reconnect with configurable delay.
- Managed publish queue through `EnqueueAsync`.
- Optional pending-message queue limit and overflow strategy.
- Subscription tracking with automatic resubscription after reconnect.
- Event hooks for connection, reconnect failures, subscription synchronization, skipped messages, processed messages, and received messages.
- Optional storage abstraction for queued messages.

## Installation

After the package is published to NuGet:

```powershell
dotnet add package IoTGateway.MQTTnet.Extensions.ManagedClient
```

Before publishing, reference the project directly:

```xml
<ProjectReference Include="..\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj" />
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
            .WithTcpServer("broker.hivemq.com", 1883)
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

Optional arguments:

```powershell
dotnet run --project src/MQTTnet.Extensions.ManagedClient.Demo/MQTTnet.Extensions.ManagedClient.Demo.csproj -- --host broker.hivemq.com --port 1883 --topic iotgateway/demo/managed-client
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
