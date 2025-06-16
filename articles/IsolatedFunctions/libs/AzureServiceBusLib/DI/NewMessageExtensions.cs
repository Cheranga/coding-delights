using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.DI;

public static class NewMessageExtensions
{
    public static IServiceCollection RegisterServiceBus(
        this IServiceCollection services,
        string connectionString,
        string? serviceBusName = null,
        Action<ServiceBusClientOptions>? options = null
    )
    {
        services.AddSingleton<IServiceBusFactory, ServiceBusFactory>();

        var specifiedServiceBusName = serviceBusName ?? ServiceBusFactory.DefaultServiceBusName;
        services.AddAzureClients(builder =>
        {
            var clientBuilder = builder.AddServiceBusClient(connectionString).WithName(specifiedServiceBusName);
            if (options != null)
            {
                clientBuilder.ConfigureOptions(options);
            }
        });

        return services;
    }

    public static OptionsBuilder<PublisherConfig<TMessage>> RegisterServiceBusPublisher<TMessage>(
        this IServiceCollection services,
        string? serviceBusName = null,
        string? publisherName = null
    )
        where TMessage : IMessage
    {
        var specifiedServiceBusName = serviceBusName ?? ServiceBusFactory.DefaultServiceBusName;
        var specifiedPublisherName = publisherName ?? typeof(TMessage).Name;

        services.AddSingleton<IServiceBusPublisher>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(specifiedPublisherName);
            var factory = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            var serviceBusClient = factory.CreateClient(specifiedServiceBusName);
            var sender = serviceBusClient.CreateSender(options.PublishTo);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ServiceBusPublisher<TMessage>>();
            var serviceBusPublisher = new ServiceBusPublisher<TMessage>(
                specifiedServiceBusName,
                specifiedPublisherName,
                options,
                sender,
                logger
            );
            return serviceBusPublisher;
        });

        services.AddSingleton<IServiceBusPublisher<TMessage>>(provider =>
        {
            var factory = provider.GetRequiredService<IServiceBusFactory>();
            var publisher = factory.GetPublisher<TMessage>(specifiedServiceBusName, specifiedPublisherName);
            return publisher;
        });

        return services.AddOptions<PublisherConfig<TMessage>>(specifiedPublisherName);
    }
}
