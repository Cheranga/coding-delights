using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

public interface IServiceBusPublisher<in TMessage>
    where TMessage : IMessage
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult>> PublishAsync(
        IReadOnlyCollection<TMessage> messages,
        CancellationToken token
    );
}
