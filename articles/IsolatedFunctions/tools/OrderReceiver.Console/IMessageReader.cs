using OrderReceiver.Console.Models;

namespace OrderReceiver.Console;

internal interface IMessageReader
{
    Task<TMessage> ReadMessageAsync<TMessage>(string topicName, string subscriptionName, CancellationToken token)
        where TMessage : IMessage;

    Task<IReadOnlyCollection<TMessage>> ReadMessageBatchAsync<TMessage>(string topicName, string subscriptionName, CancellationToken token)
        where TMessage : IMessage;
}
