namespace OrderProcessorFuncApp.Infrastructure.StorageQueues;

public interface IStorageQueuePublisher
{
    Task<OperationResponse<FailedResult, SuccessResult>> PublishAsync<TMessage>(TMessage message, CancellationToken token)
        where TMessage : class;
}
