using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace OrderPublisher.Console.Services;

internal interface IOrderGenerator<TMessage>
    where TMessage : IMessage
{
    Task<IReadOnlyList<TMessage>> GenerateOrdersAsync(int count, CancellationToken token);
}
