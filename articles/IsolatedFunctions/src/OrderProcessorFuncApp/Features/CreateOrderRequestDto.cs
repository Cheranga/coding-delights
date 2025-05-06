namespace OrderProcessorFuncApp.Features;

public sealed record CreateOrderRequestDto
{
    public required string OrderId { get; init; }
    public required string ReferenceId { get; init; }

    public required DateTimeOffset OrderDate { get; init; }

    public required IReadOnlyCollection<OrderItem> Items { get; init; }
}
