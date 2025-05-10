namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record OrderCreatedResponseDto
{
    public required string OrderId { get; init; }
    public required DateTimeOffset CreatedDateTime { get; init; }
}
