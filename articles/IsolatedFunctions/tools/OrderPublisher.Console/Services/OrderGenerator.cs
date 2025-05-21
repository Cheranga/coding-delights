using System.Collections.ObjectModel;
using AutoBogus;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal sealed class OrderGenerator : IOrderGenerator<CreateOrderMessage>
{
    private readonly AutoFaker<CreateOrderMessage> _faker;

    public OrderGenerator()
    {
        _faker = new AutoFaker<CreateOrderMessage>();
    }

    public Task<IReadOnlyList<CreateOrderMessage>> GenerateOrdersAsync(int count, CancellationToken token)
    {
        var orders = Enumerable
            .Range(1, 10)
            .SelectMany(_ =>
            {
                var orderId = Guid.NewGuid();
                return _faker.RuleFor(y => y.OrderId, orderId).Generate(count);
            })
            .ToList();
        var readOnlyCollection = new ReadOnlyCollection<CreateOrderMessage>(orders);
        return Task.FromResult<IReadOnlyList<CreateOrderMessage>>(readOnlyCollection);
    }
}
