namespace OrderReceiver.Console.Services;

public sealed record ServiceBusConfig
{
    public required string ConnectionString { get; init; }
    public required string TopicName { get; init; }
    public required string SubscriptionName { get; init; }
}
