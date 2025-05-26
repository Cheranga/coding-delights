using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal class ServiceBusTopicPublisher<TMessage> : IMessagePublisher<TMessage>
    where TMessage : IMessage
{
    private readonly ILogger<ServiceBusTopicPublisher<TMessage>> _logger;
    private readonly ServiceBusClient _serviceBusClient;
    private readonly TopicPublisherConfig<TMessage> _topicConfig;

    public ServiceBusTopicPublisher(
        ServiceBusClient serviceBusClient,
        TopicPublisherConfig<TMessage> options,
        ILogger<ServiceBusTopicPublisher<TMessage>> logger
    )
    {
        _serviceBusClient = serviceBusClient;
        _topicConfig = options;
        _logger = logger;
    }

    public async Task<OperationResponse<FailedResult, SuccessResult>> PublishToTopicAsync(TMessage message, CancellationToken token)
    {
        try
        {
            await using var sender = _serviceBusClient.CreateSender(_topicConfig.TopicName);
            var serializedMessage = JsonSerializer.Serialize(message, _topicConfig.SerializerOptions);
            var serviceBusMessage = new ServiceBusMessage(serializedMessage);
            _topicConfig.MessageOptions?.Invoke(message, serviceBusMessage);
            await sender.SendMessageAsync(serviceBusMessage, token);
            return OperationResult.Success();
        }
        catch (Exception exception)
        {
            _logger.LogError(exception, "Error occurred while publishing message to topic {TopicName}", _topicConfig.TopicName);
            return OperationResult.Failure(ErrorCodes.MessagePublishError, ErrorMessages.MessagePublishError, exception);
        }
    }

    public async Task<OperationResponse<FailedResult, SuccessResult>> PublishToTopicAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    )
    {
        await using var sender = _serviceBusClient.CreateSender(_topicConfig.TopicName);
        var addMessagesOperation = await AddMessagesToBatch(
            sender,
            messages,
            _topicConfig.SerializerOptions,
            _topicConfig.MessageOptions,
            token
        );
        var sendMessagesOperation = addMessagesOperation.Result switch
        {
            FailedResult f => f,
            OperationResult.SuccessResult<ServiceBusMessageBatch> s => await SendMessages(sender, s.Result, _logger, token),
            _ => throw new InvalidOperationException("Unexpected operation result type."),
        };

        return sendMessagesOperation;
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
            var serializedMessage = JsonSerializer.Serialize(message, serializerOptions);
            var serviceBusMessage = new ServiceBusMessage(serializedMessage);
            messageOptions?.Invoke(message, serviceBusMessage);
            if (!batch.TryAddMessage(serviceBusMessage))
                return OperationResult.Failure(ErrorCodes.TooManyMessagesInBatch, ErrorMessages.TooManyMessagesInBatch);
        }

        return OperationResult.Success(batch);
    }
}
