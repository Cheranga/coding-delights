using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;
using Microsoft.Extensions.Logging;

namespace AzureServiceBusLib.Services;

/// <summary>
/// Publishes messages to Azure Service Bus topics or queues.
/// </summary>
/// <typeparam name="TMessage">The type of message to be published.</typeparam>
/// <param name="publisherName">The unique name identifying this publisher instance.</param>
/// <param name="options">Configuration settings for the publisher.</param>
/// <param name="sender">The Azure Service Bus sender client.</param>
/// <param name="logger">Logger for publishing operations and errors.</param>
internal class ServiceBusPublisher<TMessage>(
    string publisherName,
    PublisherConfig<TMessage> options,
    ServiceBusSender sender,
    ILogger<ServiceBusPublisher<TMessage>> logger
) : IServiceBusPublisher<TMessage>
    where TMessage : IMessage
{
    /// <summary>
    /// Gets the unique name identifying this publisher instance.
    /// </summary>
    public string PublisherName { get; } = publisherName;

    /// <summary>
    /// Gets the JSON serialization options used for message serialization.
    /// Returns custom options if configured, otherwise returns default options.
    /// </summary>
    private JsonSerializerOptions SerializerOptions =>
        options.SerializerOptions
        ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    /// <summary>
    /// Publishes a collection of messages to Azure Service Bus.
    /// </summary>
    /// <param name="messages">The collection of messages to publish.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>A response indicating success or failure of the publish operation.</returns>
    public async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    )
    {
        var addMessagesOperation = await AddMessagesToBatch(sender, messages, SerializerOptions, options.MessageOptions, token);
        var sendMessagesOperation = addMessagesOperation.Result switch
        {
            OperationResult.FailedResult f => f,
            OperationResult.SuccessResult<ServiceBusMessageBatch> s => await SendMessages(sender, s.Result, logger, token),
            _ => throw new InvalidOperationException("Unexpected operation result type."),
        };

        return sendMessagesOperation;
    }

    /// <summary>
    /// Attempts to add a collection of messages to a Service Bus message batch.
    /// </summary>
    /// <param name="sender">The Service Bus sender client.</param>
    /// <param name="messages">The collection of messages to add to the batch.</param>
    /// <param name="serializerOptions">The JSON serialization options.</param>
    /// <param name="messageOptions">Optional callback to configure individual messages.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>A response containing the message batch or failure details.</returns>
    private static async Task<
        OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<ServiceBusMessageBatch>>
    > AddMessagesToBatch(
        ServiceBusSender sender,
        IReadOnlyCollection<TMessage> messages,
        JsonSerializerOptions serializerOptions,
        Action<TMessage, ServiceBusMessage>? messageOptions,
        CancellationToken token
    )
    {
        var batch = await sender.CreateMessageBatchAsync(token);
        foreach (var message in messages)
        {
            var binaryData = BinaryData.FromObjectAsJson(message, serializerOptions);
            var serviceBusMessage = new ServiceBusMessage(binaryData);
            messageOptions?.Invoke(message, serviceBusMessage);
            if (!batch.TryAddMessage(serviceBusMessage))
            {
                return OperationResult.Failure(ErrorCodes.TooManyMessagesInBatch, ErrorMessages.TooManyMessagesInBatch);
            }
        }

        return OperationResult.Success(batch);
    }

    /// <summary>
    /// Sends a batch of messages to Azure Service Bus.
    /// </summary>
    /// <param name="sender">The Service Bus sender client.</param>
    /// <param name="batch">The batch of messages to send.</param>
    /// <param name="logger">The logger for recording the operation outcome.</param>
    /// <param name="token">The cancellation token for the operation.</param>
    /// <returns>A response indicating success or failure of the send operation.</returns>
    private static async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> SendMessages(
        ServiceBusSender sender,
        ServiceBusMessageBatch batch,
        ILogger logger,
        CancellationToken token
    )
    {
        try
        {
            await sender.SendMessagesAsync(batch, token);
            logger.LogInformation("Successfully sent {MessageCount} messages to topic", batch.Count);
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ErrorMessages.MessagePublishError);
            return OperationResult.Failure(ErrorCodes.MessagePublishError, ErrorMessages.MessagePublishError, exception);
        }
    }
}
