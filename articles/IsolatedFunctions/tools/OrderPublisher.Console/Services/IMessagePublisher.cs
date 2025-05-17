using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal interface IMessagePublisher
{
    Task PublishToTopicAsync<TMessage>(string topicName, TMessage message, CancellationToken token)
        where TMessage : IMessage;

    Task PublishToTopicAsync<TMessage>(string topicName, IList<TMessage> messages, CancellationToken token)
        where TMessage : IMessage;
}
