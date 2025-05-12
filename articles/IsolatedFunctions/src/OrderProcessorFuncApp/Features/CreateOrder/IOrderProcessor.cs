using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public interface IOrderProcessor
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<OrderAcceptedData>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    );
}
