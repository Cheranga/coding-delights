using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal class MessagePublisher<TMessage, TPublisherConfig>(
    ServiceBusClient serviceBusClient,
    TPublisherConfig options,
    ILogger<MessagePublisher<TMessage, TPublisherConfig>> logger
) : IServiceBusPublisher<TMessage>
    where TMessage : IMessage
    where TPublisherConfig : BasePublisherConfig<TMessage>
{
    private JsonSerializerOptions SerializerOptions =>
        options.SerializerOptions
        ?? new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    public async Task<OperationResponse<FailedResult, SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    )
    {
        await using var sender = serviceBusClient.CreateSender(options.PublishTo);
        var addMessagesOperation = await AddMessagesToBatch(sender, messages, SerializerOptions, options.MessageOptions, token);
        var sendMessagesOperation = addMessagesOperation.Result switch
        {
            FailedResult f => f,
            OperationResult.SuccessResult<ServiceBusMessageBatch> s => await SendMessages(sender, s.Result, logger, token),
            _ => throw new InvalidOperationException("Unexpected operation result type."),
        };

        return sendMessagesOperation;
    }

    private static async Task<OperationResponse<FailedResult, OperationResult.SuccessResult<ServiceBusMessageBatch>>> AddMessagesToBatch(
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
                return OperationResult.Failure(ErrorCodes.TooManyMessagesInBatch, ErrorMessages.TooManyMessagesInBatch);
        }

        return OperationResult.Success(batch);
    }

    private static async Task<OperationResponse<FailedResult, SuccessResult>> SendMessages(
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
