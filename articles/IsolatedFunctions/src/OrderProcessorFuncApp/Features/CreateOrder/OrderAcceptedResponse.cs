using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record OrderAcceptedResponse
{
    public HttpResponseData? HttpResponse { get; set; }
}
