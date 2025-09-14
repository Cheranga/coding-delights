using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;
using OrderProcessorFuncApp.Domain.Http;
using OrderProcessorFuncApp.Infrastructure.StorageQueues;

namespace OrderProcessorFuncApp.Features.CreateOrder;

#pragma warning disable MA0048
internal interface ICreateOrderHandler
{
    Task<OperationResponse<FailedResult, SuccessResult<OrderAcceptedResponseDto>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    );
}

public sealed class CreateOrderHandler(
    [FromKeyedServices("process-order")] IStorageQueuePublisher storageQueuePublisher,
    ILogger<CreateOrderHandler> logger
) : ICreateOrderHandler
{
    public async Task<OperationResponse<FailedResult, SuccessResult<OrderAcceptedResponseDto>>> ProcessAsync(
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
                SuccessResult _ => SuccessResult<OrderAcceptedResponseDto>.New(new OrderAcceptedResponseDto(request.OrderId)),
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
#pragma warning restore MA0048
