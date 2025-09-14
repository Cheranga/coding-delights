using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain.Http;
using OrderProcessorFuncApp.Infrastructure.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class CreateOrderEndPointFunction(
    IApiRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator> requestReader,
    ILogger<CreateOrderEndPointFunction> logger
)
{
    [Function(nameof(CreateOrderEndPointFunction))]
    public async Task<OrderAcceptedResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        logger.LogInformation("starting to process order");
        var token = context.CancellationToken;
        var readOperation = await requestReader.ReadRequestAsync(req, token);
        if (readOperation.Result is FailedResult failedResult)
        {
            var errorResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await errorResponse.WriteAsJsonAsync(failedResult.Error, token);
            return new OrderAcceptedResponse { HttpResponse = errorResponse };
        }
        var dto = readOperation.MapSuccess();
        var response = await ProcessOrderAsync(req, dto, context.CancellationToken);
        logger.LogInformation("finished processing order");
        return response;
    }

    private async Task<OrderAcceptedResponse> ProcessOrderAsync(HttpRequestData request, CreateOrderRequestDto dto, CancellationToken token)
    {
        var message = dto.ToMessage();
        var httpResponse = request.CreateResponse(HttpStatusCode.Accepted);
        await httpResponse.WriteAsJsonAsync(new OrderAcceptedResponseDto(dto.OrderId), token);
        var response = new OrderAcceptedResponse { HttpResponse = httpResponse, Message = message };

        return response;
    }
}
