using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

internal interface IMessagePublisher
{
    Task PublishToTopicAsync<TMessage>(string topicName, TMessage message, CancellationToken token)
        where TMessage : ISessionMessage;

    Task PublishToTopicAsync<TMessage>(string topicName, IList<TMessage> messages, CancellationToken token)
        where TMessage : ISessionMessage;
}
