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
    public async Task<OperationResponse<FailedResult, SuccessResult<TMessage>>> ReadMessageAsync(
        QueueMessage message,
        CancellationToken token
    )
    {
        try
        {
            var data = await JsonSerializer.DeserializeAsync<TMessage>(message.Body.ToStream(), serializerOptions, token);
            if (data is not null)
            {
                return SuccessResult<TMessage>.New(data);
            }

            var failedResult = FailedResult.New(ErrorCodes.InvalidMessageSchema, ErrorMessages.InvalidMessageSchema);
            return failedResult;
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "An error occurred while deserializing the message");
            return FailedResult.New(ErrorCodes.InvalidMessageSchema, ErrorMessages.InvalidMessageSchema);
        }
    }
}
