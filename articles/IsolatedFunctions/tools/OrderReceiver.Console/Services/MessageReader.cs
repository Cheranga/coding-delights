using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OrderReceiver.Console.Models;

namespace OrderReceiver.Console.Services;

internal sealed class MessageReader(ServiceBusClient serviceBusClient, JsonSerializerOptions serializerOptions) : IMessageReader
{
    public async Task<TMessage> ReadMessageAsync<TMessage>(string topicName, string subscriptionName, CancellationToken token)
        where TMessage : IMessage
    {
        var receiver = await serviceBusClient.AcceptNextSessionAsync(
            topicName: topicName,
            subscriptionName: subscriptionName,
            cancellationToken: token
        );
        var serviceBusMessage = await receiver.ReceiveMessageAsync(cancellationToken: token);
        var message = serviceBusMessage.Body.ToObjectFromJson<TMessage>(serializerOptions);
        await receiver.CompleteMessageAsync(serviceBusMessage, token);
        await receiver.CloseAsync(token);
        return message!;
    }

    public async Task<IReadOnlyList<TMessage>> ReadMessageBatchAsync<TMessage>(
        string topicName,
        string subscriptionName,
        CancellationToken token
    )
        where TMessage : IMessage
    {
        var receiver = await serviceBusClient.AcceptNextSessionAsync(
            topicName: topicName,
            subscriptionName: subscriptionName,
            options: new ServiceBusSessionReceiverOptions { ReceiveMode = ServiceBusReceiveMode.PeekLock },
            cancellationToken: token
        );

        var messages = new List<TMessage>();
        while (true)
        {
            var serviceBusMessages = await receiver.ReceiveMessagesAsync(5, maxWaitTime: TimeSpan.FromSeconds(2), cancellationToken: token);
            if (serviceBusMessages.Count == 0)
            {
                break;
            }

            foreach (var serviceBusMessage in serviceBusMessages)
            {
                messages.Add(serviceBusMessage.Body.ToObjectFromJson<TMessage>(serializerOptions)!);
                await receiver.CompleteMessageAsync(serviceBusMessage, token);
            }
        }

        await receiver.CloseAsync(token);

        return messages;
    }
}
