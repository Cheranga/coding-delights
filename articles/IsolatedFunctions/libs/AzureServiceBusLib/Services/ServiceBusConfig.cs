namespace AzureServiceBusLib.Services;

internal sealed record ServiceBusConfig
{
    public required string ConnectionString { get; init; }
    public required string TopicName { get; init; }
}
