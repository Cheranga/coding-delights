namespace OrderProcessorFuncApp.Configs;

internal sealed record StorageConfig
{
    public required string ConnectionString { get; init; }
    public required string ProcessingQueueName { get; init; }
}
