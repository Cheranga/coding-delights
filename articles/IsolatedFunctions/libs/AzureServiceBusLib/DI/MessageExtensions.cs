using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.DI;

public static class MessageExtensions
{
    public static OptionsBuilder<PublisherConfig<TMessage>> RegisterServiceBusPublisher<TMessage>(
        this IServiceCollection services,
        string? publisherName = null
    )
        where TMessage : IMessage
    {
        var specifiedPublisherName = publisherName ?? typeof(TMessage).Name;

        services.AddSingleton<IServiceBusPublisher>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(specifiedPublisherName);

            var serviceBusClient = options.ServiceBusClientOptions is null
                ? new ServiceBusClient(options.ConnectionString)
                : new ServiceBusClient(options.ConnectionString, options.ServiceBusClientOptions);
            var sender = serviceBusClient.CreateSender(options.PublishTo);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ServiceBusPublisher<TMessage>>();
            var serviceBusPublisher = new ServiceBusPublisher<TMessage>(specifiedPublisherName, options, sender, logger);
            return serviceBusPublisher;
        });

        services.AddSingleton<IServiceBusPublisher<TMessage>>(provider =>
        {
            var factory = provider.GetRequiredService<IServiceBusFactory>();
            var publisher = factory.GetPublisher<TMessage>(specifiedPublisherName);
            return publisher;
        });

        return services.AddOptions<PublisherConfig<TMessage>>(specifiedPublisherName);
    }

    public static IServiceCollection RegisterServiceBus(this IServiceCollection services)
    {
        services.AddSingleton<IServiceBusFactory, ServiceBusFactory>();

        return services;
    }
}
