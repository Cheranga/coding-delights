using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class CreateOrderFunction(
    IOrderProcessor orderProcessor,
    IOrderApiResponseGenerator responseGenerator,
    ILogger<CreateOrderFunction> logger
)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<HttpResponseData> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        logger.LogInformation("starting to process order");
        if (context.Items["Dto"] is CreateOrderRequestDto dto)
        {
            var response = await ProcessOrderAsync(req, dto, context.CancellationToken);
            logger.LogInformation("finished processing order");
            return response;
        }

        logger.LogError("DTO not found in context items");
        return req.CreateResponse(HttpStatusCode.InternalServerError);
    }

    private async Task<HttpResponseData> ProcessOrderAsync(HttpRequestData request, CreateOrderRequestDto dto, CancellationToken token)
    {
        var operation = await orderProcessor.ProcessAsync(dto, token);
        var response = await operation.Match(
            x => responseGenerator.GenerateErrorResponseAsync(request, x, HttpStatusCode.InternalServerError, token),
            x => responseGenerator.GenerateOrderAcceptedResponseAsync(request, x.Result.OrderId, token)
        );

        return response;
    }
}
