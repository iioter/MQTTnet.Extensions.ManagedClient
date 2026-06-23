using MQTTnet;
using MQTTnet.Extensions.ManagedClient;
using MQTTnet.Protocol;

var settings = DemoSettings.Parse(args);
using var cancellation = new CancellationTokenSource();

Console.CancelKeyPress += (_, eventArgs) =>
{
    eventArgs.Cancel = true;
    cancellation.Cancel();
};

var factory = new MqttClientFactory();
using var client = factory.CreateManagedMqttClient();

client.ConnectedAsync += eventArgs =>
{
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Connected. Session present: {eventArgs.ConnectResult.IsSessionPresent}");
    return Task.CompletedTask;
};

client.DisconnectedAsync += eventArgs =>
{
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Disconnected. Reason: {eventArgs.Reason}");
    return Task.CompletedTask;
};

client.ConnectingFailedAsync += eventArgs =>
{
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Connecting failed: {eventArgs.Exception.Message}");
    return Task.CompletedTask;
};

client.ApplicationMessageReceivedAsync += eventArgs =>
{
    var payload = eventArgs.ApplicationMessage.ConvertPayloadToString();
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Received {eventArgs.ApplicationMessage.Topic}: {payload}");
    return Task.CompletedTask;
};

client.ApplicationMessageProcessedAsync += eventArgs =>
{
    var status = eventArgs.Exception == null ? "published" : $"failed: {eventArgs.Exception.Message}";
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Message {eventArgs.ApplicationMessage.Id} {status}");
    return Task.CompletedTask;
};

client.ApplicationMessageSkippedAsync += eventArgs =>
{
    Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Message skipped: {eventArgs.ApplicationMessage.Id}");
    return Task.CompletedTask;
};

var options = new ManagedMqttClientOptionsBuilder()
    .WithAutoReconnectDelay(TimeSpan.FromSeconds(5))
    .WithMaxPendingMessages(1000)
    .WithPendingMessagesOverflowStrategy(MqttPendingMessagesOverflowStrategy.DropOldestQueuedMessage)
    .WithClientOptions(builder =>
    {
        builder
            .WithTcpServer(settings.Host, settings.Port)
            .WithCredentials("admin", "iotgateway.net")
            .WithClientId(settings.ClientId)
            .WithCleanSession();
    })
    .Build();

Console.WriteLine("MQTTnet.Extensions.ManagedClient demo");
Console.WriteLine($"Broker : {settings.Host}:{settings.Port}");
Console.WriteLine($"Topic  : {settings.Topic}");
Console.WriteLine($"Client : {settings.ClientId}");
Console.WriteLine("Press Ctrl+C to stop.");

await client.StartAsync(options);
await client.SubscribeAsync(settings.Topic, MqttQualityOfServiceLevel.AtLeastOnce);

var messageIndex = 0;

try
{
    while (!cancellation.IsCancellationRequested)
    {
        messageIndex++;
        var payload = $"demo message {messageIndex} at {DateTimeOffset.Now:O}";

        await client.EnqueueAsync(
            settings.Topic,
            payload,
            MqttQualityOfServiceLevel.AtLeastOnce);

        Console.WriteLine($"[{DateTimeOffset.Now:HH:mm:ss}] Enqueued: {payload}");
        await Task.Delay(settings.PublishInterval, cancellation.Token);
    }
}
catch (OperationCanceledException)
{
}
finally
{
    Console.WriteLine("Stopping managed MQTT client...");
    await client.StopAsync();
}

internal sealed record DemoSettings(
    string Host,
    int Port,
    string Topic,
    string ClientId,
    TimeSpan PublishInterval)
{
    public static DemoSettings Parse(string[] args)
    {
        var host = GetValue(args, "--host") ?? "iotgateway.net";
        var topic = GetValue(args, "--topic") ?? "iotgateway/demo/managed-client";
        var clientId = GetValue(args, "--client-id") ?? $"managed-client-demo-{Guid.NewGuid():N}";
        var port = TryGetInt(args, "--port") ?? 1883;
        var publishIntervalSeconds = TryGetInt(args, "--interval") ?? 5;

        return new DemoSettings(
            host,
            port,
            topic,
            clientId,
            TimeSpan.FromSeconds(publishIntervalSeconds));
    }

    static string? GetValue(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (StringComparer.OrdinalIgnoreCase.Equals(args[i], name))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    static int? TryGetInt(string[] args, string name)
    {
        var value = GetValue(args, name);
        return int.TryParse(value, out var result) ? result : null;
    }
}
