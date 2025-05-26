using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal interface IMessagePublisher
{
    Task PublishToTopicAsync<TMessage>(string topicName, TMessage message, CancellationToken token)
        where TMessage : ISessionMessage;

    Task PublishToTopicAsync<TMessage>(string topicName, IList<TMessage> messages, CancellationToken token)
        where TMessage : ISessionMessage;
}

internal interface IMessagePublisher<in TMessage>
    where TMessage : IMessage
{
    Task<OperationResponse<FailedResult, SuccessResult>> PublishToTopicAsync(TMessage message, CancellationToken token);

    Task<OperationResponse<FailedResult, SuccessResult>> PublishToTopicAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    );
}
