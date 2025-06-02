using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

//
// Remove this class, after testing ServiceBusTopicPublisher
//
internal sealed class MessagePublisher(ServiceBusClient serviceBusClient, JsonSerializerOptions serializerOptions) : IMessagePublisher
{
    public async Task PublishToTopicAsync<TSessionMessage>(string topicName, TSessionMessage message, CancellationToken token)
        where TSessionMessage : ISessionMessage
    {
        await using var sender = serviceBusClient.CreateSender(topicName);
        var serializedMessage = JsonSerializer.Serialize(message, serializerOptions);
        await sender.SendMessageAsync(
            new ServiceBusMessage(serializedMessage) { SessionId = message.SessionId, Subject = message.MessageType },
            token
        );
    }

    public async Task PublishToTopicAsync<TSessionMessage>(string topicName, IList<TSessionMessage> messages, CancellationToken token)
        where TSessionMessage : ISessionMessage
    {
        await using var sender = serviceBusClient.CreateSender(topicName);
        var messageBatch = await sender.CreateMessageBatchAsync(token);
        foreach (var message in messages)
        {
            var serializedMessage = JsonSerializer.Serialize(message, serializerOptions);
            var serviceBusMessage = new ServiceBusMessage(serializedMessage)
            {
                SessionId = message.SessionId,
                Subject = message.MessageType,
            };
            var canEnqueue = messageBatch.TryAddMessage(serviceBusMessage);
            if (!canEnqueue)
            {
                break;
            }
        }

        await sender.SendMessagesAsync(messageBatch, token);
    }
}
