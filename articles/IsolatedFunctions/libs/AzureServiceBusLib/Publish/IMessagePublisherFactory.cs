using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Publish;

public interface IMessagePublisherFactory
{
    public IMessagePublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage;

    public IMessagePublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage;
}
