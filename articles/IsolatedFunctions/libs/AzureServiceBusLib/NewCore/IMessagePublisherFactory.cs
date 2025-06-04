using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.NewCore;

public interface IMessagePublisherFactory
{
    public IServiceBusMessagePublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage;

    public IServiceBusMessagePublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage;
}
