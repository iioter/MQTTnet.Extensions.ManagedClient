# MQTTnet.Extensions.ManagedClient

[English](README.md)

在工业物联网、边缘网关、设备采集和业务系统对接场景中，MQTT 客户端通常不是“连上一次就结束”的短连接工具，而是需要长期在线、持续发布和订阅消息的基础组件。真实现场经常会遇到网络抖动、Broker 重启、设备侧断网、订阅丢失、短时间消息堆积等问题。如果这些逻辑都由业务代码自行处理，应用会很快充满重连循环、状态判断、队列管理和异常恢复代码。

`MQTTnet.Extensions.ManagedClient` 将这些通用问题封装为一个可复用的托管客户端：应用只需要配置连接、订阅主题并把消息加入发布队列，客户端会负责连接维护、断线后自动重连、重连后恢复订阅，以及在弱网或离线期间有序处理待发布消息。它帮助开发者把精力放在业务数据和设备逻辑上，而不是反复编写 MQTT 连接可靠性代码。

`MQTTnet.Extensions.ManagedClient` 是面向 MQTTnet 5.x 的托管 MQTT 客户端扩展。它负责维持连接、自动重连、重连后恢复订阅，并通过托管队列发布应用消息。

它尤其适合：

- 工业网关、边缘计算节点、数据采集服务等需要 7x24 小时运行的 .NET 应用。
- 需要在网络不稳定时保持发布链路可恢复的设备上报场景。
- 需要在重连后自动恢复订阅关系的命令下发、遥测接收和状态同步场景。
- 希望统一观察连接、重连失败、订阅同步、消息跳过和消息处理结果的应用。

## 功能特性

- 支持自动重连，并可配置重连间隔。
- 通过 `EnqueueAsync` 使用托管发布队列。
- 支持配置待发布消息上限和队列溢出策略。
- 跟踪订阅关系，重连后自动重新订阅。
- 提供连接、重连失败、订阅同步、消息跳过、消息处理、消息接收等事件。
- 提供可选的待发布消息存储抽象。

## 安装

NuGet ：

```xml
<PackageReference Include="IoTGateway.MQTTnet.Extensions.ManagedClient" Version="5.0.0" />
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

运行 Demo 项目：

```powershell
dotnet run --project src/MQTTnet.Extensions.ManagedClient.Demo/MQTTnet.Extensions.ManagedClient.Demo.csproj
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
