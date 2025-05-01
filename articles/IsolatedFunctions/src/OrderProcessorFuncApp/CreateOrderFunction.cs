using System.Net;
using System.Text.Json;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Features;

namespace OrderProcessorFuncApp;

public class CreateOrderFunction(
    IValidator<CreateOrderRequestDto> validator,
    JsonSerializerOptions serializerOptions,
    ILogger<CreateOrderFunction> logger
)
{
    [Function(nameof(CreateOrderFunction))]
    public async Task<OrderAcceptedResponse> Run(
        [HttpTrigger(AuthorizationLevel.Function, "post", Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        var token = context.CancellationToken;
        var dto = await GetDtoFromRequest(req, serializerOptions, token);
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

        logger.LogInformation("Received {@CreateOrderRequest}", dto);
        // Do processing
        return await req.CreateSuccessResponse(HttpStatusCode.Accepted, new OrderAcceptedData(dto.OrderId), serializerOptions, token);
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
