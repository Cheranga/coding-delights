using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

public interface IServiceBusMessagePublisher
{
    public string Name { get; }
}

public interface IServiceBusMessagePublisher<TMessage> : IServiceBusMessagePublisher
    where TMessage : IMessage
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    );
}
