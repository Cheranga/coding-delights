using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

public interface IServiceBusPublisher<in TMessage>
    where TMessage : IMessage
{
    Task<OperationResponse<FailedResult, SuccessResult>> PublishAsync(IReadOnlyCollection<TMessage> messages, CancellationToken token);
}
