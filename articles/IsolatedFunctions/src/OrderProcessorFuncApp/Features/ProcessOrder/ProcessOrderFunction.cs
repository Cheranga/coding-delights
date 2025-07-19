using System;
using System.Text.Json;
using Azure.Storage.Queues.Models;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp.Features.ProcessOrder;

public class ProcessOrderFunction
{
    private readonly ILogger<ProcessOrderFunction> _logger;

    public ProcessOrderFunction(ILogger<ProcessOrderFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(ProcessOrderFunction))]
    public async Task Run(
        [QueueTrigger("%StorageConfig:ProcessingQueueName%", Connection = "StorageConfig:ConnectionString")] QueueMessage message
    )
    {
        await using var stream = message.Body.ToStream();
        using var reader = new StreamReader(stream);
        var messageContent = await reader.ReadToEndAsync();
        await Task.Delay(TimeSpan.FromSeconds(1));
        _logger.LogInformation("Processing order message: {Message}", messageContent);
    }
}
