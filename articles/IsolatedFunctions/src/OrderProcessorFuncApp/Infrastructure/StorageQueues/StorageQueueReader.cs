using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;

namespace OrderProcessorFuncApp.Infrastructure.StorageQueues;

internal sealed class StorageQueueReader<TMessage>(JsonSerializerOptions serializerOptions, ILogger<StorageQueueReader<TMessage>> logger)
    : IStorageQueueReader<TMessage>
    where TMessage : class
{
    public Task<OperationResponse<FailedResult, SuccessResult<TMessage>>> ReadMessageAsync(QueueMessage message, CancellationToken token)
    {
        try
        {
            var data = JsonSerializer.Deserialize<TMessage>(message.MessageText, serializerOptions);
            if (data is not null)
            {
                return Task.FromResult<OperationResponse<FailedResult, SuccessResult<TMessage>>>(SuccessResult<TMessage>.New(data));
            }

            var failedResult = FailedResult.New(ErrorCodes.InvalidMessageSchema, ErrorMessages.InvalidMessageSchema);
            return Task.FromResult<OperationResponse<FailedResult, SuccessResult<TMessage>>>(failedResult);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "An error occurred while deserializing the message");
            return Task.FromResult<OperationResponse<FailedResult, SuccessResult<TMessage>>>(
                FailedResult.New(ErrorCodes.InvalidMessageSchema, ErrorMessages.InvalidMessageSchema)
            );
        }
    }
}
