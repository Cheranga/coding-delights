using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record OrderApiResponse
{
    public HttpResponseData? HttpResponse { get; set; }
}
