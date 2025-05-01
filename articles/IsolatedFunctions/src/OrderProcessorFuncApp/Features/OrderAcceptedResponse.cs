using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Features;

public sealed record OrderAcceptedResponse
{
    public HttpResponseData? HttpResponse { get; set; }
}
