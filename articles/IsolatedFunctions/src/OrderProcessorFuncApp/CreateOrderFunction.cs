using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Features;

namespace OrderProcessorFuncApp;

public class CreateOrderFunction(ILogger<CreateOrderFunction> logger)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        var token = context.CancellationToken;
        var dto = await req.ReadFromJsonAsync<CreateOrderRequestDto>(token);
        logger.LogInformation("Received {@OrderRequest}", dto);
        return req.CreateResponse(HttpStatusCode.Accepted);
    }
}
