using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;
using ShotCaller.Azure.ServiceBus.Messaging.Services;

namespace OrderPublisher.Console;

internal class OrderPublisherBackgroundService(
    IOptions<ServiceBusConfig> options,
    IOrderGenerator<CreateOrderMessage> orderGenerator,
    IServiceBusPublisher<CreateOrderMessage> orderPublisher,
    IServiceBusFactory publisherFactory,
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

                //
                // using the injected IServiceBusPublisher<CreateOrderMessage> directly
                //
                var operation = await orderPublisher.PublishAsync(orders, stoppingToken);
                _ = operation.Result switch
                {
                    SuccessResult _ => LogSuccess(topicName),
                    FailedResult failure => LogFailure(topicName, failure),
                    _ => LogError(topicName),
                };

                //
                // using the IServiceBusFactory to get a named publisher
                //
                operation = await publisherFactory.GetPublisher<CreateOrderMessage>("orders").PublishAsync(orders, stoppingToken);
                _ = operation.Result switch
                {
                    SuccessResult _ => LogSuccess(topicName),
                    FailedResult failure => LogFailure(topicName, failure),
                    _ => LogError(topicName),
                };

                //
                // using the IServiceBusFactory to get another named publisher
                //
                operation = await publisherFactory.GetPublisher<CreateOrderMessage>("q-orders").PublishAsync(orders, stoppingToken);
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
