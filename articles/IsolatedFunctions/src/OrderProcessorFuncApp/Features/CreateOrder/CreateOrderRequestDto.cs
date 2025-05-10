namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record CreateOrderRequestDto
{
    public required Guid OrderId { get; init; }
    public required Guid ReferenceId { get; init; }
    public required DateTimeOffset OrderDate { get; init; }

    public required IReadOnlyCollection<OrderItem> Items { get; init; }
}
