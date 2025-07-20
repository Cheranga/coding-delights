using OrderProcessorFuncApp.Domain.Models;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal interface IOrderProcessor
{
    Task<OperationResponse<FailedResult, SuccessResult<OrderAcceptedResponse>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    );
}
