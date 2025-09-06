using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Domain.Messaging;

namespace OrderProcessorFuncApp.Features.ProcessOrder;

public class ProcessOrderFunction
{
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly ILogger<ProcessOrderFunction> _logger;

    public ProcessOrderFunction(JsonSerializerOptions serializerOptions, ILogger<ProcessOrderFunction> logger)
    {
        _serializerOptions = serializerOptions;
        _logger = logger;
    }

    [Function(nameof(ProcessOrderFunction))]
    public async Task Run([QueueTrigger("%StorageConfig:ProcessingQueueName%", Connection = "QueueConnection")] QueueMessage message)
    {
        var processOrderMessage = message.Body.ToObjectFromJson<ProcessOrderMessage>(_serializerOptions);
        ArgumentNullException.ThrowIfNull(processOrderMessage);
        await Task.Delay(TimeSpan.FromSeconds(1));
        _logger.LogInformation(
            "Processing order message: {ReferenceId} with {@Message}",
            processOrderMessage.ReferenceId,
            processOrderMessage
        );
    }
}
