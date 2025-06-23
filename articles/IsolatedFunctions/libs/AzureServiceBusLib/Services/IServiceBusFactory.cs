using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Services;

/// <summary>
/// Factory interface responsible for creating and managing Azure Service Bus publishers.
/// </summary>
public interface IServiceBusFactory
{
    /// <summary>
    /// Creates or retrieves a publisher for the specified message type using the message type name as publisher name.
    /// </summary>
    /// <typeparam name="TMessage">The type of message that will be published.</typeparam>
    /// <returns>An instance of IServiceBusPublisher for the specified message type.</returns>
    /// <exception cref="MessagePublisherNotFoundException{TMessage}">Thrown when a publisher for the specified message type is not found.</exception>
    IServiceBusPublisher<TMessage> GetPublisher<TMessage>()
        where TMessage : IMessage;

    /// <summary>
    /// Creates or retrieves a publisher for the specified message type and publisher name.
    /// </summary>
    /// <typeparam name="TMessage">The type of message that will be published.</typeparam>
    /// <param name="publisherName">The name of the publisher to retrieve.</param>
    /// <returns>An instance of IServiceBusPublisher for the specified message type and publisher name.</returns>
    /// <exception cref="MessagePublisherNotFoundException{TMessage}">Thrown when a publisher with the specified name is not found.</exception>
    IServiceBusPublisher<TMessage> GetPublisher<TMessage>(string publisherName)
        where TMessage : IMessage;
}
