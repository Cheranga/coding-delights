using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record CreateOrderApiResponse
{
    public HttpResponseData? HttpResponse { get; set; }
}
