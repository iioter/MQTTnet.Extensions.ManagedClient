# MQTTnet.Extensions.ManagedClient

[English](README.md)

`MQTTnet.Extensions.ManagedClient` 是面向 MQTTnet 8.x 的托管 MQTT 客户端扩展。它负责维持连接、自动重连、重连后恢复订阅，并通过托管队列发布应用消息。

## 功能特性

- 支持自动重连，并可配置重连间隔。
- 通过 `EnqueueAsync` 使用托管发布队列。
- 支持配置待发布消息上限和队列溢出策略。
- 跟踪订阅关系，重连后自动重新订阅。
- 提供连接、重连失败、订阅同步、消息跳过、消息处理、消息接收等事件。
- 提供可选的待发布消息存储抽象。

## 安装

发布到 NuGet 后可以这样安装：

```powershell
dotnet add package IoTGateway.MQTTnet.Extensions.ManagedClient
```

发布前可以直接引用项目：

```xml
<ProjectReference Include="..\MQTTnet.Extensions.ManagedClient\MQTTnet.Extensions.ManagedClient.csproj" />
```

## 快速开始

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

运行 Demo 项目：

```powershell
dotnet run --project src/MQTTnet.Extensions.ManagedClient.Demo/MQTTnet.Extensions.ManagedClient.Demo.csproj
```

可选参数：

```powershell
dotnet run --project src/MQTTnet.Extensions.ManagedClient.Demo/MQTTnet.Extensions.ManagedClient.Demo.csproj -- --host broker.hivemq.com --port 1883 --topic iotgateway/demo/managed-client
```

Demo 会连接到 MQTT Broker，订阅指定主题，通过托管队列发布消息，打印接收到的消息，并在按下 `Ctrl+C` 后正常停止。

## 主要 API

- `MqttClientFactory.CreateManagedMqttClient()`：创建托管客户端。
- `ManagedMqttClientOptionsBuilder`：构建重连、队列、存储和 MQTT 客户端选项。
- `IManagedMqttClient.StartAsync(...)`：启动连接维护。
- `IManagedMqttClient.EnqueueAsync(...)`：将应用消息加入发布队列。
- `IManagedMqttClient.SubscribeAsync(...)` 和 `UnsubscribeAsync(...)`：管理订阅。
- `IManagedMqttClient.StopAsync(...)`：停止发布并断开连接。

## License

This project is licensed under the [MIT License](LICENSE).
