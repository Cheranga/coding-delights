using System.Text.Json;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;

namespace OrderProcessorFuncApp.Infrastructure.StorageQueues;

internal sealed class StorageQueuePublisher(
    QueueClient queueClient,
    JsonSerializerOptions serializerOptions,
    ILogger<StorageQueuePublisher> logger
) : IStorageQueuePublisher
{
    public async Task<OperationResponse<FailedResult, SuccessResult>> PublishAsync<TMessage>(TMessage message, CancellationToken token)
        where TMessage : class
    {
        try
        {
            var qMessage = BinaryData.FromObjectAsJson(message, serializerOptions);
            var operation = await queueClient.SendMessageAsync(qMessage, cancellationToken: token);
            if (operation.GetRawResponse().IsError)
            {
                logger.LogError("Failed to publish message to queue: {Message}", operation.GetRawResponse().ReasonPhrase);
                return FailedResult.New(ErrorCodes.ErrorOccurredWhenPublishingToQueue, ErrorMessages.ErrorOccurredWhenProcessingOrder);
            }

            logger.LogInformation("Message published to queue successfully: {MessageId}", operation.Value.MessageId);
            return SuccessResult.New();
        }
        catch (Exception exception)
        {
            logger.LogError(exception, ErrorMessages.ErrorOccurredWhenPublishingToQueue);
            return FailedResult.New(ErrorCodes.ErrorOccurredWhenPublishingToQueue, exception.Message);
        }
    }
}
