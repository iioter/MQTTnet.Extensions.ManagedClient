namespace MQTTnet.Extensions.ManagedClient
{
    public enum MqttPendingMessagesOverflowStrategy
    {
        DropNewMessage,
        DropOldestQueuedMessage
    }
}
