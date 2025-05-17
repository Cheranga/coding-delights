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
        var orderId = Guid.NewGuid();
        var orders = _faker.RuleFor(x => x.OrderId, orderId).Generate(count);
        var readOnlyCollection = new ReadOnlyCollection<CreateOrderMessage>(orders);
        return Task.FromResult<IReadOnlyList<CreateOrderMessage>>(readOnlyCollection);
    }
}
