namespace OrderProcessorFuncApp.Features;

internal sealed record CreateOrderRequestLoggingDto(string OrderId, string ReferenceId, DateTimeOffset OrderDate, List<OrderItem> Items)
{
    public static CreateOrderRequestLoggingDto New(CreateOrderRequestDto dto) =>
        new(dto.OrderId, dto.ReferenceId, dto.OrderDate, dto.Items.ToList());
}
