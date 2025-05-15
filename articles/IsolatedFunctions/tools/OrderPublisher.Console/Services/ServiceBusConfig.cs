namespace OrderPublisher.Console.Services;

public sealed record ServiceBusConfig
{
    public required string ConnectionString { get; init; }
    public required string TopicName { get; init; }
}
