namespace OrderProcessorFuncApp.Domain.Models;

public sealed record ProcessOrderMessage
{
    public required Guid ReferenceId { get; init; }
    public required IReadOnlyCollection<OrderItem> Items { get; init; }

    public required DateTimeOffset OrderReceivedAt { get; init; }
}
