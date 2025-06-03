using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

internal interface IServiceBusReader<TMessage>
    where TMessage : IMessage
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<TMessage>>> ReadMessageAsync(
        string topicName,
        string subscriptionName,
        CancellationToken token
    );

    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<TMessage>>> ReadMessageAsync(
        string queueName,
        CancellationToken token
    );
}
