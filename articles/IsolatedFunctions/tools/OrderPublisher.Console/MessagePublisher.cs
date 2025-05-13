using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console;

internal sealed class MessagePublisher(ServiceBusClient serviceBusClient, JsonSerializerOptions serializerOptions) : IMessagePublisher
{
    public Task PublishToTopicAsync<TMessage>(string topicName, TMessage message, CancellationToken token)
        where TMessage : IMessage
    {
        var sender = serviceBusClient.CreateSender(topicName);
        var serializedMessage = JsonSerializer.Serialize(message, serializerOptions);
        return sender.SendMessageAsync(
            new ServiceBusMessage(serializedMessage) { SessionId = message.Id, Subject = message.MessageType },
            token
        );
    }

    public async Task PublishToTopicAsync<TMessage>(string topicName, IReadOnlyCollection<TMessage> messages, CancellationToken token)
        where TMessage : IMessage
    {
        var sender = serviceBusClient.CreateSender(topicName);
        var messageBatch = await sender.CreateMessageBatchAsync(token);
        foreach (var message in messages)
        {
            var serializedMessage = JsonSerializer.Serialize(message, serializerOptions);
            var serviceBusMessage = new ServiceBusMessage(serializedMessage) { SessionId = message.Id, Subject = message.MessageType };
            var canEnqueue = messageBatch.TryAddMessage(serviceBusMessage);
            if (!canEnqueue)
            {
                break;
            }
        }

        await sender.SendMessagesAsync(messageBatch, token);
    }
}
