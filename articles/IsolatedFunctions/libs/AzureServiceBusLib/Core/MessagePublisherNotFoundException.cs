/// <summary>
/// Contains core components for handling Azure Service Bus messaging exceptions.
/// </summary>
namespace AzureServiceBusLib.Core;

/// <summary>
/// Represents an exception that is thrown when a publisher for a specific message type cannot be found.
/// </summary>
/// <typeparam name="TMessage">The type of message for which a publisher was not found. Must implement <see cref="IMessage"/>.</typeparam>
public sealed class MessagePublisherNotFoundException<TMessage> : Exception
    where TMessage : IMessage
{
    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePublisherNotFoundException{TMessage}"/> class with a specified publisher name.
    /// </summary>
    /// <param name="publisherName">The name of the publisher that was not found.</param>
    public MessagePublisherNotFoundException(string publisherName)
        : base($"There's no publisher registered for message type {typeof(TMessage).Name} with the name {publisherName}") { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePublisherNotFoundException{TMessage}"/> class.
    /// Uses the message type name as the publisher name.
    /// </summary>
    public MessagePublisherNotFoundException()
        : this(typeof(TMessage).Name) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MessagePublisherNotFoundException{TMessage}"/> class with a custom error message and inner exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public MessagePublisherNotFoundException(string? message, Exception? innerException)
        : base(message, innerException) { }
}
