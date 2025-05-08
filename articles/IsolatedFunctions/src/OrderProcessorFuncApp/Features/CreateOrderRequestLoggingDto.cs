namespace OrderProcessorFuncApp.Features;

internal sealed record CreateOrderRequestLoggingDto
{
    public required string OrderId { get; set; }
    public required string ReferenceId { get; set; }
    public required DateTimeOffset OrderDate { get; set; }
    public required List<OrderItem> Items { get; set; }

    public static CreateOrderRequestLoggingDto New(CreateOrderRequestDto dto) =>
        new()
        {
            OrderId = dto.OrderId,
            ReferenceId = dto.ReferenceId,
            OrderDate = dto.OrderDate,
            Items = dto.Items.ToList(),
        };
}
