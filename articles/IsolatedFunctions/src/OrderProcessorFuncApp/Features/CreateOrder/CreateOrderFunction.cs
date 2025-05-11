using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Http;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public class CreateOrderFunction(
    ITestHttpRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator> requestReader,
    IOrderProcessor orderProcessor,
    IOrderApiResponseGenerator responseGenerator,
    ILogger<CreateOrderFunction> logger
)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<OrderAcceptedResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        logger.LogInformation("starting to process order");
        var token = context.CancellationToken;
        var readOperation = await requestReader.ReadRequestAsync(req, token);
        var response = readOperation.Result switch
        {
            OperationResult.FailedResult f => await responseGenerator.GenerateErrorResponseAsync(req, f, HttpStatusCode.BadRequest, token),
            OperationResult.SuccessResult<CreateOrderRequestDto> s => await ProcessOrderAsync(req, s.Result, token),
            _ => await responseGenerator.GenerateUnknownErrorAsync(req, token),
        };

        logger.LogInformation("finished processing order");
        return response;
    }

    private async Task<OrderAcceptedResponse> ProcessOrderAsync(HttpRequestData request, CreateOrderRequestDto dto, CancellationToken token)
    {
        var operation = await orderProcessor.ProcessAsync(dto, token);
        var response = operation.Result switch
        {
            OperationResult.FailedResult f => await responseGenerator.GenerateErrorResponseAsync(
                request,
                f,
                HttpStatusCode.BadRequest,
                token
            ),
            OperationResult.SuccessResult<OrderAcceptedData> s => await responseGenerator.GenerateOrderAcceptedResponseAsync(
                request,
                s.Result.OrderId
            ),
            _ => await responseGenerator.GenerateUnknownErrorAsync(request, token),
        };

        return response;
    }
}
