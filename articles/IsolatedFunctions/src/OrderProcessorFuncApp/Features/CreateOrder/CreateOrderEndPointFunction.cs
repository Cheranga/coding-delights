using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;
using OrderProcessorFuncApp.Infrastructure.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class CreateOrderEndPointFunction(IValidator<CreateOrderRequestDto> validator, ILogger<CreateOrderEndPointFunction> logger)
{
    [Function(nameof(CreateOrderEndPointFunction))]
    public async Task<OrderAcceptedResponse> Run(
        [HttpTrigger(AuthorizationLevel.Anonymous, WebRequestMethods.Http.Post, Route = "orders")] HttpRequestData req,
        FunctionContext context
    )
    {
        logger.LogInformation("starting to process order");
        var token = context.CancellationToken;
        var dto = await req.ReadFromJsonAsync<CreateOrderRequestDto>(token);
        if (dto is null)
        {
            var invalidDtoSchemaResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await invalidDtoSchemaResponse.WriteAsJsonAsync(
                ErrorResponse.New(ErrorCodes.InvalidRequestSchema, ErrorMessages.InvalidRequestSchema),
                token
            );
            return new OrderAcceptedResponse { HttpResponse = invalidDtoSchemaResponse };
        }

        var validationResult = await validator.ValidateAsync(dto, token);
        if (!validationResult.IsValid)
        {
            var invalidDtoResponse = req.CreateResponse(HttpStatusCode.BadRequest);
            await invalidDtoResponse.WriteAsJsonAsync(
                ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest, validationResult),
                token
            );
            return new OrderAcceptedResponse { HttpResponse = invalidDtoResponse };
        }

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
