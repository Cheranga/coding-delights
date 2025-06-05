using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using Microsoft.Extensions.Logging;

namespace AzureServiceBusLib.Publish;

internal sealed class MessagePublisher<TMessage>(
    string publisherName,
    PublisherConfig<TMessage> options,
    ILogger<MessagePublisher<TMessage>> logger
) : IMessagePublisher<TMessage>
    where TMessage : IMessage
{
    public string Name { get; } = publisherName;

    private JsonSerializerOptions SerializerOptions =>
        options.SerializerOptions
        ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    public async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    )
    {
        await using var client = new ServiceBusClient(options.ConnectionString);
        await using var sender = client.CreateSender(options.PublishTo);
        var addMessagesOperation = await AddMessagesToBatch(sender, messages, SerializerOptions, options.MessageOptions, token);
        var sendMessagesOperation = addMessagesOperation.Result switch
        {
            OperationResult.FailedResult f => f,
            OperationResult.SuccessResult<ServiceBusMessageBatch> s => await SendMessages(sender, s.Result, logger, token),
            _ => throw new InvalidOperationException("Unexpected operation result type."),
        };

        return sendMessagesOperation;
    }

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
