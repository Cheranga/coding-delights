using AzureServiceBusLib.Core;

namespace OrderPublisher.Console.Services;

internal interface IOrderGenerator<TMessage>
    where TMessage : ISessionMessage
{
    Task<IReadOnlyList<TMessage>> GenerateOrdersAsync(int count, CancellationToken token);
}
