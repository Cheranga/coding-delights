using FluentValidation;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed class OrderProcessor(IValidator<CreateOrderRequestDto> validator, ILogger<OrderProcessor> logger) : IOrderProcessor
{
    public async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<OrderAcceptedData>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    )
    {
        var validationResult = await validator.ValidateAsync(request, token);
        if (!validationResult.IsValid)
        {
            logger.LogError("Invalid order request: {@ValidationResult}", validationResult);
            return OperationResult.FailedResult.New(
                ErrorCodes.InvalidCreateOrderRequest,
                ErrorMessages.InvalidCreateOrderRequest,
                validationResult
            );
        }

        logger.LogInformation("Received order request: {@Request}", request);

        // do processing
        return OperationResult.SuccessResult<OrderAcceptedData>.New(new OrderAcceptedData(request.OrderId));
    }
}
