using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.NewCore;

public class MessagePublisherFactory : IMessagePublisherFactory
{
    private readonly Dictionary<string, IServiceBusMessagePublisher> _publishersMappedByName;

    public MessagePublisherFactory(IEnumerable<IServiceBusMessagePublisher> publishers)
    {
        _publishersMappedByName = publishers.GroupBy(x => x.Name).ToDictionary(x => x.Key, x => x.First());
    }

    public IServiceBusMessagePublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage => GetPublisher<TMessage>(typeof(TMessage).Name);

    public IServiceBusMessagePublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage
    {
        if (_publishersMappedByName.TryGetValue(publisherName, out var pub) && pub is IServiceBusMessagePublisher<TMessage> publisher)
        {
            return publisher;
        }

        throw new Exception($"There's no publisher registered for {publisherName}");
    }
}
