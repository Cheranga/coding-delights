using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderReceiver.Console.Models;
using OrderReceiver.Console.Services;

namespace OrderReceiver.Console;

internal class OrderReaderBackgroundService(
    IOptions<ServiceBusConfig> options,
    IMessageReader messageReader,
    ILogger<OrderReaderBackgroundService> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var serviceBusConfig = options.Value;
        var topicName = serviceBusConfig.TopicName;
        var subscriptionName = serviceBusConfig.SubscriptionName;
        while (!stoppingToken.IsCancellationRequested)
        {
            var orders = await messageReader.ReadMessageBatchAsync<CreateOrderMessage>(topicName, subscriptionName, stoppingToken);
            logger.LogInformation(
                "Received {Count} messages from topic {TopicName} and subscription {SubscriptionName}",
                orders.Count,
                topicName,
                subscriptionName
            );
            await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
        }
    }
}
