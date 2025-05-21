using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderReceiver.Console.Models;

namespace OrderReceiver.Console.Services;

internal sealed class MessageReader(
    ServiceBusClient serviceBusClient,
    JsonSerializerOptions serializerOptions,
    ILogger<MessageReader> logger
) : IMessageReader
{
    private const int MaxMessageCount = 5;
    private static readonly TimeSpan MaxWaitTime = TimeSpan.FromSeconds(2);

    public async Task<TMessage?> ReadMessageAsync<TMessage>(string topicName, string subscriptionName, CancellationToken token)
        where TMessage : IMessage
    {
        await using var receiver = await serviceBusClient.AcceptNextSessionAsync(
            topicName: topicName,
            subscriptionName: subscriptionName,
            cancellationToken: token
        );
        var serviceBusMessage = await receiver.ReceiveMessageAsync(cancellationToken: token);
        try
        {
            var message = serviceBusMessage.Body.ToObjectFromJson<TMessage>(serializerOptions);
            await receiver.CompleteMessageAsync(serviceBusMessage, token);
            return message!;
        }
        catch (JsonException exception)
        {
            logger.LogError(
                exception,
                "Error occurred while deserializing message from topic {TopicName} and subscription {SubscriptionName}",
                topicName,
                subscriptionName
            );
            await receiver.DeadLetterMessageAsync(
                serviceBusMessage,
                deadLetterReason: "DeserializationError",
                deadLetterErrorDescription: exception.Message,
                cancellationToken: token
            );
        }
        catch (Exception exception)
        {
            logger.LogWarning(
                exception,
                "Error occurred while receiving message from topic {TopicName} and subscription {SubscriptionName}. Current {DeliveryCount}",
                topicName,
                subscriptionName,
                serviceBusMessage.DeliveryCount
            );
            await receiver.AbandonMessageAsync(serviceBusMessage, cancellationToken: token);
        }

        return default;
    }

    public async Task<IReadOnlyList<TMessage>> ReadMessageBatchAsync<TMessage>(
        string topicName,
        string subscriptionName,
        CancellationToken token
    )
        where TMessage : IMessage
    {
        var messages = new List<TMessage>();
        await using var receiver = await serviceBusClient.AcceptNextSessionAsync(
            topicName: topicName,
            subscriptionName: subscriptionName,
            options: new ServiceBusSessionReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock },
            cancellationToken: token
        );
        try
        {
            while (true)
            {
                try
                {
                    var serviceBusMessages = await receiver.ReceiveMessagesAsync(
                        MaxMessageCount,
                        maxWaitTime: MaxWaitTime,
                        cancellationToken: token
                    );
                    if (serviceBusMessages.Count == 0)
                    {
                        break;
                    }

                    foreach (var serviceBusMessage in serviceBusMessages)
                    {
                        try
                        {
                            messages.Add(serviceBusMessage.Body.ToObjectFromJson<TMessage>(serializerOptions)!);
                            await receiver.CompleteMessageAsync(serviceBusMessage, token);
                        }
                        catch (Exception exception)
                        {
                            logger.LogError(
                                exception,
                                "Error occurred while deserializing message from topic {TopicName} and subscription {SubscriptionName}",
                                topicName,
                                subscriptionName
                            );
                            await receiver.AbandonMessageAsync(serviceBusMessage, cancellationToken: token);
                        }
                    }
                }
                catch (Exception exception)
                {
                    logger.LogError(
                        exception,
                        "Error occurred while receiving messages from topic {TopicName} and subscription {SubscriptionName}",
                        topicName,
                        subscriptionName
                    );
                }
            }
        }
        catch (Exception exception)
        {
            logger.LogError(
                exception,
                "Error occurred while reading messages from topic {TopicName} and subscription {SubscriptionName}",
                topicName,
                subscriptionName
            );
        }

        return messages;
    }
}
