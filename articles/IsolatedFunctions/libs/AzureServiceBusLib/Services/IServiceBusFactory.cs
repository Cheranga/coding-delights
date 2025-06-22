using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Services;

public interface IServiceBusFactory
{
    IServiceBusPublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage;

    IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage;
}
