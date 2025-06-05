namespace AzureServiceBusLib.Tests.Configs;

public sealed record ServiceBusConfig
{
    public required string ConnectionString { get; init; }
    public required string TopicName { get; init; }

    public required string QueueName { get; init; }
}
