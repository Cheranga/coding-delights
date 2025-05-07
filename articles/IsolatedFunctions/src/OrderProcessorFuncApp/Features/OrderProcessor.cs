using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp.Features;

internal sealed class OrderProcessor(ILogger<OrderProcessor> logger) : IOrderProcessor
{
    
    public async Task ProcessAsync(CreateOrderRequestDto request, CancellationToken token)
    {
        await Task.Delay(TimeSpan.FromSeconds(2), token);
        logger.LogInformation("Processing order with ID: {OrderId}", request.OrderId);
    }
}