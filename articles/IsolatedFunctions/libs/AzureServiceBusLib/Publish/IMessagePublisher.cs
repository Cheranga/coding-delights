using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Publish;

public interface IMessagePublisher
{
    public string Name { get; }
}

public interface IMessagePublisher<in TMessage> : IMessagePublisher
    where TMessage : IMessage
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    );
}
