namespace OrderProcessorFuncApp.Features.ProcessOrder;

internal sealed record CustomerCreatedEvent
{
    public required string Id { get; init; }
    public required string Name { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
}
