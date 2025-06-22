using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Publish;

internal sealed class MessagePublisherFactory : IMessagePublisherFactory
{
    private readonly Dictionary<string, IMessagePublisher> _publishersMappedByName;

    public MessagePublisherFactory(IEnumerable<IMessagePublisher> publishers) =>
        _publishersMappedByName = publishers
            .GroupBy(x => x.Name, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);

    public IMessagePublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage => GetPublisher<TMessage>(typeof(TMessage).Name);

    public IMessagePublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage
    {
        if (_publishersMappedByName.TryGetValue(publisherName, out var pub) && pub is IMessagePublisher<TMessage> publisher)
        {
            return publisher;
        }

        throw new MessagePublisherNotFoundException<TMessage>(publisherName);
    }
}
