using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class CreateOrderFunction(
    IApiRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator> requestReader,
    OrderProcessor orderProcessor,
    IOrderApiResponseGenerator responseGenerator,
    JsonSerializerOptions serializerOptions,
    ILogger<CreateOrderFunction> logger
)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<CreateOrderApiResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        logger.LogInformation("starting to process order");
        var token = context.CancellationToken;
        var readOperation = await requestReader.ReadRequestAsync(req, token);
        var response = await readOperation.Match(
            x => GenerateErrorResponse(req, x, HttpStatusCode.BadRequest, token),
            x => ProcessOrderAsync(req, x.Result, token)
        );

        logger.LogInformation("finished processing order");
        return response;
    }

    private Task<CreateOrderApiResponse> GenerateErrorResponse(
        HttpRequestData req,
        FailedResult x,
        HttpStatusCode statusCode,
        CancellationToken token
    ) => responseGenerator.GenerateErrorResponseAsync(req, x.Error, statusCode, serializerOptions, token);

    private async Task<CreateOrderApiResponse> ProcessOrderAsync(
        HttpRequestData request,
        CreateOrderRequestDto dto,
        CancellationToken token
    )
    {
        var operation = await orderProcessor.ProcessAsync(dto, token);
        var response = await operation.Match(
            x => GenerateErrorResponse(request, x, HttpStatusCode.InternalServerError, token),
            x => responseGenerator.GenerateOrderAcceptedResponseAsync(request, x.Result.OrderId, serializerOptions, token)
        );

        return response;
    }
}
