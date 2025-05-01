using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
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
        if (dto is null)
        {
            logger.LogWarning("Create order request does not contain any data to proceed");
            return await req.CreateErrorResponse(
                ErrorCodes.InvalidRequestSchema,
                ErrorMessages.InvalidRequestSchema,
                HttpStatusCode.BadRequest
            );
        }
        await Task.Delay(TimeSpan.FromSeconds(1), token);
        logger.LogInformation("C# HTTP trigger function processed a request");
        return req.CreateResponse(HttpStatusCode.Accepted);
    }
}
