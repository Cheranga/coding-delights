using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Models;
using AzureServiceBusLib.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.Core;

internal class MessageClientBuilder : IMessageClientBuilder
{
    private readonly IServiceCollection _services;

    public MessageClientBuilder(IServiceCollection services)
    {
        _services = services;
    }

    public OptionsBuilder<TopicPublisherConfig<TMessage>> AddTopicPublisher<TMessage>()
        where TMessage : IMessage
    {
        var publisherName = typeof(TMessage).Name;
        _services.TryAddSingleton<ITopicPublisher<TMessage>>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<TopicPublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(publisherName);
            var serviceBusClient = new ServiceBusClient(options.ConnectionString);
            var logger = provider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<TopicPublisher<TMessage, TopicPublisherConfig<TMessage>>>();

            var publisher = new TopicPublisher<TMessage, TopicPublisherConfig<TMessage>>(serviceBusClient, options, logger);
            return publisher;
        });

        return _services.AddOptions<TopicPublisherConfig<TMessage>>(publisherName);
    }

    public OptionsBuilder<QueuePublisherConfig<TMessage>> AddQueuePublisher<TMessage>()
        where TMessage : IMessage
    {
        var publisherName = typeof(TMessage).Name;
        _services.TryAddSingleton<IQueuePublisher<TMessage>>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<QueuePublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(publisherName);
            var serviceBusClient = new ServiceBusClient(options.ConnectionString);
            var logger = provider
                .GetRequiredService<ILoggerFactory>()
                .CreateLogger<QueuePublisher<TMessage, QueuePublisherConfig<TMessage>>>();

            var publisher = new QueuePublisher<TMessage, QueuePublisherConfig<TMessage>>(serviceBusClient, options, logger);
            return publisher;
        });
        return _services.AddOptions<QueuePublisherConfig<TMessage>>(publisherName);
    }
}
