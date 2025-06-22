using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Services;

internal sealed class ServiceBusFactory : IServiceBusFactory
{
    private readonly Dictionary<string, IServiceBusPublisher> _publishersMappedByNameInServiceBuses;

    public ServiceBusFactory(IEnumerable<IServiceBusPublisher> publishers)
    {
        _publishersMappedByNameInServiceBuses = publishers.GroupBy(x => x.PublisherName).ToDictionary(x => x.Key, x => x.First());
    }

    public IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage
    {
        if (
            _publishersMappedByNameInServiceBuses.TryGetValue(publisherName, out var publisher)
            && publisher is IServiceBusPublisher<TMessage> typedPublisher
        )
        {
            return typedPublisher;
        }

        throw new MessagePublisherNotFoundException<TMessage>(publisherName);
    }

    public IServiceBusPublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage => GetPublisher<TMessage>(typeof(TMessage).Name);
}
