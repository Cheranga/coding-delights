﻿using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Features.CreateOrder;

internal sealed class OrderProcessor(ILogger<OrderProcessor> logger) : IOrderProcessor
{
    public async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<OrderAcceptedData>>> ProcessAsync(
        CreateOrderRequestDto request,
        CancellationToken token
    )
    {
        try
        {
            // Simulating some processing time
            await Task.Delay(TimeSpan.FromSeconds(1), token);
            return OperationResult.SuccessResult<OrderAcceptedData>.New(new OrderAcceptedData(request.OrderId));
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "An error occurred while processing the order request");
        }

        return OperationResult.FailedResult.New(
            ErrorCodes.ErrorOccurredWhenProcessingOrder,
            ErrorMessages.ErrorOccurredWhenProcessingOrder
        );
    }
}
