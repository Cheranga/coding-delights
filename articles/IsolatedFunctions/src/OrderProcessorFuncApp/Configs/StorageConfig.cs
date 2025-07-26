namespace OrderProcessorFuncApp.Configs;

internal sealed record StorageConfig
{
    public required string Connection { get; init; }
    public required string ProcessingQueueName { get; init; }
}
