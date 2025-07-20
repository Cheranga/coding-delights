using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Infrastructure.StorageQueues;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class OrderProcessor(
    [FromKeyedServices("process-order")] IStorageQueuePublisher storageQueuePublisher,
    ILogger<OrderProcessor> logger
) : IOrderProcessor
{
    public async Task<OperationResponse<FailedResult, SuccessResult<OrderAcceptedResponse>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    )
    {
        try
        {
            var processOrderMessage = request.ToMessage();
            var operation = await storageQueuePublisher.PublishAsync(processOrderMessage, token);
            return operation.Result switch
            {
                FailedResult f => f,
                SuccessResult _ => SuccessResult<OrderAcceptedResponse>.New(new OrderAcceptedResponse(request.OrderId)),
                _ => FailedResult.New(ErrorCodes.Unknown, ErrorMessages.Unknown),
            };
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while processing the order request");
        }

        return FailedResult.New(ErrorCodes.ErrorOccurredWhenProcessingOrder, ErrorMessages.ErrorOccurredWhenProcessingOrder);
    }
}
