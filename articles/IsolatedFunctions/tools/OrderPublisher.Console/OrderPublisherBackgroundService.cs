using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;

namespace OrderPublisher.Console;

internal class OrderPublisherBackgroundService(
    IOptions<ServiceBusConfig> options,
    IOrderGenerator<CreateOrderMessage> orderGenerator,
    IMessagePublisher<CreateOrderMessage> messagePublisher,
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
                await messagePublisher.PublishToTopicAsync(orders, stoppingToken);
                logger.LogInformation("Published {Count} messages to topic {TopicName}", orders.Count, topicName);
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error occurred while publishing messages to topic {TopicName}", topicName);
        }
    }
}
