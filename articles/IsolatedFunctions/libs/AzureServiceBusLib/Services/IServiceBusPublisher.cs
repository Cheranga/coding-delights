using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Services;

/// <summary>
/// Defines the base contract for Azure Service Bus message publishers.
/// </summary>
public interface IServiceBusPublisher
{
    /// <summary>
    /// Gets the unique name identifier for the publisher instance.
    /// </summary>
    public string PublisherName { get; }
}

/// <summary>
/// Defines a strongly-typed publisher for sending messages to Azure Service Bus.
/// </summary>
/// <typeparam name="TMessage">The type of messages this publisher can handle.</typeparam>
public interface IServiceBusPublisher<in TMessage> : IServiceBusPublisher
    where TMessage : IMessage
{
    /// <summary>
    /// Publishes a collection of messages to Azure Service Bus.
    /// </summary>
    /// <param name="messages">The collection of messages to be published.</param>
    /// <param name="token">A cancellation token to cancel the operation.</param>
    /// <returns>A task representing the asynchronous operation with success or failure result.</returns>
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    );
}
