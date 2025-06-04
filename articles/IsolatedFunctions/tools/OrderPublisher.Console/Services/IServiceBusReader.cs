using AzureServiceBusLib.Models;
using AzureServiceBusLib.NewCore;

namespace OrderPublisher.Console.Services;

internal interface IServiceBusReader<TMessage>
    where TMessage : IMessage
{
    Task<OperationResponse<FailedResult, OperationResult.SuccessResult<TMessage>>> ReadMessageAsync(
        string topicName,
        string subscriptionName,
        CancellationToken token
    );

    Task<OperationResponse<FailedResult, OperationResult.SuccessResult<TMessage>>> ReadMessageAsync(
        string queueName,
        CancellationToken token
    );
}
