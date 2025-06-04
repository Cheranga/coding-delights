using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

public interface IMessagePublisherFactory
{
    public IServiceBusMessagePublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage;

    public IServiceBusMessagePublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage;
}
