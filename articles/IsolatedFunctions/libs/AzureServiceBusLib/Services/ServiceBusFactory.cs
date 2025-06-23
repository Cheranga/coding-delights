using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Services;

/// <summary>
/// Factory class for managing and retrieving Azure Service Bus publishers.
/// Provides methods to get typed publishers for specific message types.
/// </summary>
internal sealed class ServiceBusFactory : IServiceBusFactory
{
    /// <summary>
    /// Dictionary mapping publisher names to their corresponding service bus publisher instances.
    /// Publishers are stored using case-sensitive name comparison.
    /// </summary>
    private readonly Dictionary<string, IServiceBusPublisher> _publishersMappedByNameInServiceBuses;

    /// <summary>
    /// Initializes a new instance of the <see cref="ServiceBusFactory"/> class.
    /// Groups publishers by name and stores the first publisher for each unique name.
    /// </summary>
    /// <param name="publishers">Collection of service bus publishers to be managed by the factory.</param>
    public ServiceBusFactory(IEnumerable<IServiceBusPublisher> publishers)
    {
        _publishersMappedByNameInServiceBuses = publishers
            .GroupBy(x => x.PublisherName, StringComparer.Ordinal)
            .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
    }

    /// <summary>
    /// Retrieves a strongly-typed publisher for the specified message type using the provided publisher name.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to be published, must implement <see cref="IMessage"/>.</typeparam>
    /// <param name="publisherName">The name of the publisher to retrieve.</param>
    /// <returns>A strongly-typed service bus publisher for the specified message type.</returns>
    /// <exception cref="MessagePublisherNotFoundException{TMessage}">Thrown when no publisher is found for the specified name and message type.</exception>
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

    /// <summary>
    /// Retrieves a strongly-typed publisher for the specified message type using the message type name as the publisher name.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to be published, must implement <see cref="IMessage"/>.</typeparam>
    /// <returns>A strongly-typed service bus publisher for the specified message type.</returns>
    /// <exception cref="MessagePublisherNotFoundException{TMessage}">Thrown when no publisher is found for the message type.</exception>
    public IServiceBusPublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage => GetPublisher<TMessage>(typeof(TMessage).Name);
}
