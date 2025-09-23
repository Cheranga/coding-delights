using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain.Messaging;
using OrderProcessorFuncApp.Infrastructure.StorageQueues;

namespace OrderProcessorFuncApp.Features.ProcessOrder;

public class ProcessOrderFunction(IStorageQueueReader<ProcessOrderMessage> queueReader, ILogger<ProcessOrderFunction> logger)
{
    [Function(nameof(ProcessOrderFunction))]
    public async Task Run(
        [QueueTrigger("%StorageConfig:ProcessingQueueName%", Connection = "QueueConnection")] QueueMessage message,
        FunctionContext context
    )
    {
        var token = context.CancellationToken;
        var readMessageOperation = await queueReader.ReadMessageAsync(message, token);
        if (readMessageOperation.Result is FailedResult failedResult)
        {
            logger.LogError("Failed to read message from queue. {@FailedResult}", failedResult);
            //
            // Unlike in service bus, we don't have control of the message actions.
            // So you must throw here to ensure the message ends up in the DLQ.
            //
            throw new InvalidOperationException("Failed to read message from queue");
        }

        var processOrderMessage = readMessageOperation.MapSuccess();
        logger.LogInformation(
            "Processing order message: {ReferenceId} with {@Message}",
            processOrderMessage.ReferenceId,
            processOrderMessage
        );
    }
}
