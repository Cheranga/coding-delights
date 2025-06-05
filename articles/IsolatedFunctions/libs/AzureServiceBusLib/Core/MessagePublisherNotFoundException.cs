using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Core;

public sealed class MessagePublisherNotFoundException<TMessage> : Exception
    where TMessage : IMessage
{
    public MessagePublisherNotFoundException(string publisherName)
        : base($"There's no publisher registered for message type {typeof(TMessage).Name} with the name {publisherName}") { }

    public MessagePublisherNotFoundException()
        : base($"There's no publisher registered for message type {typeof(TMessage).Name}") { }
}
