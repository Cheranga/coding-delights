namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record CreateOrderApiResponse
{
    public required HttpResponseMessage HttpResponse { get; set; }
}
