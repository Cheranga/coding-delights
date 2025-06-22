using System.ComponentModel;
using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Services;

[EditorBrowsable(EditorBrowsableState.Never)]
public interface IServiceBusPublisher
{
    public string PublisherName { get; }
}

public interface IServiceBusPublisher<in TMessage> : IServiceBusPublisher
    where TMessage : IMessage
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    );
}
