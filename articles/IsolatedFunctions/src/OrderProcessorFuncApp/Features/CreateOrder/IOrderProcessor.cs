namespace OrderProcessorFuncApp.Features.CreateOrder;

public interface IOrderProcessor
{
    Task<OperationResponse<FailedResult, SuccessResult<OrderAcceptedData>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    );
}
