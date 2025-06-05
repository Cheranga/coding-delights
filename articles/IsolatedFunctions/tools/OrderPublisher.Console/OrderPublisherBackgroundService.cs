using AzureServiceBusLib.Publish;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;

namespace OrderPublisher.Console;

internal class OrderPublisherBackgroundService(
    IOptions<ServiceBusConfig> options,
    IOrderGenerator<CreateOrderMessage> orderGenerator,
    IMessagePublisher<CreateOrderMessage> orderPublisher,
    IMessagePublisherFactory publisherFactory,
    ILogger<OrderPublisherBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceBusConfig = options.Value;
        var topicName = serviceBusConfig.TopicName;

        try
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var orders = await orderGenerator.GenerateOrdersAsync(10, stoppingToken);
                var operation = await orderPublisher.PublishAsync(orders, stoppingToken);
                _ = operation.Result switch
                {
                    SuccessResult _ => LogSuccess(topicName),
                    FailedResult failure => LogFailure(topicName, failure),
                    _ => LogError(topicName),
                };

                operation = await publisherFactory.GetPublisher<CreateOrderMessage>().PublishAsync(orders, stoppingToken);
                _ = operation.Result switch
                {
                    SuccessResult _ => LogSuccess(topicName),
                    FailedResult failure => LogFailure(topicName, failure),
                    _ => LogError(topicName),
                };

                operation = await publisherFactory.GetPublisher<CreateOrderMessage>("q-orders").PublishAsync(orders, stoppingToken);
                _ = operation.Result switch
                {
                    SuccessResult _ => LogSuccess(topicName),
                    FailedResult failure => LogFailure(topicName, failure),
                    _ => LogError(topicName),
                };

                operation = await publisherFactory.GetPublisher<CreateOrderMessage>().PublishAsync(orders, stoppingToken);
                _ = operation.Result switch
                {
                    SuccessResult _ => LogSuccess(topicName),
                    FailedResult failure => LogFailure(topicName, failure),
                    _ => LogError(topicName),
                };

                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error occurred while publishing messages to topic {TopicName}", topicName);
        }
    }

    private Unit LogError(string topicName)
    {
        logger.LogError("Unexpected result while publishing messages to topic {TopicName}", topicName);
        return Unit.Instance;
    }

    private Unit LogFailure(string topicName, FailedResult failure)
    {
        logger.LogError("Failed to publish messages to topic {TopicName}: {ErrorMessage}", topicName, failure.ErrorMessage);

        return Unit.Instance;
    }

    private Unit LogSuccess(string topicName)
    {
        logger.LogInformation("Successfully published messages to topic {TopicName}", topicName);
        return Unit.Instance;
    }
}
