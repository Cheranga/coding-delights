using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using Serilog.Context;

namespace OrderProcessorFuncApp.Features;

public class CreateOrderFunction(
    IOrderProcessor orderProcessor,
    IValidator<CreateOrderRequestDto> validator,
    JsonSerializerOptions serializerOptions,
    ILogger<CreateOrderFunction> logger
)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<OrderAcceptedResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        // var correlationId = req.Headers.TryGetValues("x-correlation-id", out var values)
        //     ? values.FirstOrDefault()
#pragma warning disable S125
        //     : Guid.NewGuid().ToString("N");
#pragma warning restore S125

        // using (LogContext.PushProperty("CorrelationId", correlationId))
        // {
        var token = context.CancellationToken;
        var dto = await GetDtoFromRequest(req, serializerOptions, token);
        logger.LogInformation("Received {@CreateOrderRequest}", dto);
        var validationResult = await validator.ValidateAsync(dto, token);
        if (!validationResult.IsValid)
        {
            logger.LogWarning("Invalid {@CreateOrderRequest} received Validation failed {@ValidationResult}", dto, validationResult);
            return await req.CreateErrorResponse(
                ErrorCodes.InvalidCreateOrderRequest,
                ErrorMessages.InvalidCreateOrderRequest,
                HttpStatusCode.BadRequest,
                serializerOptions,
                token
            );
        }

        logger.LogInformation("Validation passed");
        await orderProcessor.ProcessAsync(dto, token);
        // Do processing
        return await req.CreateSuccessResponse(HttpStatusCode.Accepted, new OrderAcceptedData(dto.OrderId), serializerOptions, token);
#pragma warning disable S125
        // }
#pragma warning restore S125
    }

    private static async Task<CreateOrderRequestDto> GetDtoFromRequest(
        HttpRequestData request,
        JsonSerializerOptions serializerOptions,
        CancellationToken token
    )
    {
        await using var stream = request.Body;
        using var reader = new StreamReader(stream);
        var content = await reader.ReadToEndAsync(token);
        var dto = JsonSerializer.Deserialize<CreateOrderRequestDto>(content, serializerOptions);
        return dto!;
    }
}
