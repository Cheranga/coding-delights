using System.Net;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class CreateOrderFunction(
    IApiRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator> requestReader,
    IOrderProcessor orderProcessor,
    IOrderApiResponseGenerator responseGenerator,
    ILogger<CreateOrderFunction> logger
)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<OrderApiResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        logger.LogInformation("starting to process order");
        var token = context.CancellationToken;
        var readOperation = await requestReader.ReadRequestAsync(req, token);
        var response = readOperation.Result switch
        {
            FailedResult f => await GenerateErrorResponse(req, f.Error, token),
            SuccessResult<CreateOrderRequestDto> s => await ProcessOrderAsync(req, s.Result, token),
            _ => await GenerateErrorResponse(req, ErrorResponse.New(ErrorCodes.Unknown, ErrorMessages.Unknown), token),
        };

        logger.LogInformation("finished processing order");
        return response;
    }

    private Task<OrderApiResponse> GenerateErrorResponse(HttpRequestData req, ErrorResponse error, CancellationToken token) =>
        responseGenerator.GenerateErrorResponseAsync(req, error, HttpStatusCode.BadRequest, token);

    private async Task<OrderApiResponse> ProcessOrderAsync(HttpRequestData request, CreateOrderRequestDto dto, CancellationToken token)
    {
        var operation = await orderProcessor.ProcessAsync(dto, token);
        var response = operation.Result switch
        {
            FailedResult f => await responseGenerator.GenerateErrorResponseAsync(request, f.Error, HttpStatusCode.BadRequest, token),
            SuccessResult<OrderAcceptedData> s => await responseGenerator.GenerateOrderAcceptedResponseAsync(
                request,
                s.Result.OrderId,
                token
            ),
            _ => await GenerateErrorResponse(request, ErrorResponse.New(ErrorCodes.Unknown, ErrorMessages.Unknown), token),
        };

        return response;
    }
}
