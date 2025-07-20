using OrderProcessorFuncApp.Domain.Models;

namespace OrderProcessorFuncApp.Domain.Messaging;

public sealed record ProcessOrderMessage
{
    public required Guid ReferenceId { get; init; }
    public required IReadOnlyCollection<OrderItem> Items { get; init; }

    public required DateTimeOffset OrderReceivedAt { get; init; }
}
