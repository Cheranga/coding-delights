using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp;

public class CreateOrderFunction(ILogger<CreateOrderFunction> logger)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        await Task.Delay(TimeSpan.FromSeconds(1));
        logger.LogInformation("C# HTTP trigger function processed a request");
        return req.CreateResponse(HttpStatusCode.Accepted);
    }
}
