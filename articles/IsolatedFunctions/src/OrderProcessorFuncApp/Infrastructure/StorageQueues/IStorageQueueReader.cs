using Azure.Storage.Queues.Models;
using OrderProcessorFuncApp.Core;

namespace OrderProcessorFuncApp.Infrastructure.StorageQueues;

public interface IStorageQueueReader<TMessage>
    where TMessage : class
{
    Task<OperationResponse<FailedResult, SuccessResult<TMessage>>> ReadMessageAsync(QueueMessage message, CancellationToken token);
}
