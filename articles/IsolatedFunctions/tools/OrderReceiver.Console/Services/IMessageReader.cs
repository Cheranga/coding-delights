using OrderReceiver.Console.Models;

namespace OrderReceiver.Console.Services;

internal interface IMessageReader
{
    Task<TMessage?> ReadMessageAsync<TMessage>(string topicName, string subscriptionName, CancellationToken token)
        where TMessage : IMessage;

    Task<IReadOnlyList<TMessage>> ReadMessageBatchAsync<TMessage>(string topicName, string subscriptionName, CancellationToken token)
        where TMessage : IMessage;
}
