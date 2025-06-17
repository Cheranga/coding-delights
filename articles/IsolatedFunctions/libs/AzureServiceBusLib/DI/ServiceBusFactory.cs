using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.DI;

internal sealed class ServiceBusFactory : IServiceBusFactory
{
    private readonly Dictionary<string, Dictionary<string, IServiceBusPublisher>> _publishersMappedByNameInServiceBuses;

    public ServiceBusFactory(IEnumerable<IServiceBusPublisher> publishers)
    {
        _publishersMappedByNameInServiceBuses = publishers
            .GroupBy(x => x.ServiceBusName)
            .ToDictionary(x => x.Key, x => x.GroupBy(y => y.PublisherName).ToDictionary(z => z.Key, z => z.First()));
    }

    public IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string serviceBusName, string publisherName)
        where TMessage : IMessage
    {
        if (
            _publishersMappedByNameInServiceBuses.TryGetValue(serviceBusName, out var publishers)
            && publishers.TryGetValue(publisherName, out var publisher)
            && publisher is IServiceBusPublisher<TMessage> typedPublisher
        )
        {
            return typedPublisher;
        }

        throw new MessagePublisherNotFoundException<TMessage>(publisherName);
    }

    public IServiceBusPublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage => GetPublisher<TMessage>(NewMessageExtensions.DefaultServiceBusName, typeof(TMessage).Name);

    public IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage => GetPublisher<TMessage>(NewMessageExtensions.DefaultServiceBusName, publisherName);
}
