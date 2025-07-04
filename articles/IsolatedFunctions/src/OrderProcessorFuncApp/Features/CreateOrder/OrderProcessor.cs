using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class OrderProcessor(ILogger<OrderProcessor> logger) : IOrderProcessor
{
    public async Task<OperationResponse<FailedResult, SuccessResult<OrderAcceptedData>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    )
    {
        try
        {
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            return SuccessResult<OrderAcceptedData>.New(new OrderAcceptedData(request.OrderId));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while processing the order request");
        }

        return FailedResult.New(ErrorCodes.ErrorOccurredWhenProcessingOrder, ErrorMessages.ErrorOccurredWhenProcessingOrder);
    }
}
