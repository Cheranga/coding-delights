namespace OrderProcessorFuncApp.Features;

public interface IOrderProcessor
{
    Task ProcessAsync(CreateOrderRequestDto request, CancellationToken token);
}
