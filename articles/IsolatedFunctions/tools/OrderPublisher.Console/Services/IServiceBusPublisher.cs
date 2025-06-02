using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal interface IServiceBusPublisher<in TMessage>
    where TMessage : IMessage
{
    Task<OperationResponse<FailedResult, SuccessResult>> PublishAsync(TMessage message, CancellationToken token);

    Task<OperationResponse<FailedResult, SuccessResult>> PublishAsync(IReadOnlyCollection<TMessage> messages, CancellationToken token);
}
