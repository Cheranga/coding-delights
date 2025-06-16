using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.DI;

public interface IServiceBusFactory
{
    IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string serviceBusName, string publisherName)
        where TMessage : IMessage;

    IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage;
}
