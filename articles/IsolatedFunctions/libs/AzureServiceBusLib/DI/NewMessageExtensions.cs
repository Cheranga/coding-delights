using System.ComponentModel.DataAnnotations;
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
        [Required] string serviceBusName,
        [Required] string connectionString,
        Action<ServiceBusClientOptions>? options = null
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(serviceBusName, nameof(serviceBusName));
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

        services.AddSingleton<IServiceBusFactory, ServiceBusFactory>();

        services.AddAzureClients(builder =>
        {
            var clientBuilder = builder.AddServiceBusClient(connectionString).WithName(serviceBusName);
            if (options != null)
            {
                clientBuilder.ConfigureOptions(options);
            }
        });

        return services;
    }

    public static OptionsBuilder<PublisherConfig<TMessage>> RegisterServiceBusPublisher<TMessage>(
        this IServiceCollection services,
        [Required] string serviceBusName,
        string? publisherName = null
    )
        where TMessage : IMessage
    {
        var specifiedPublisherName = publisherName ?? typeof(TMessage).Name;

        services.AddSingleton<IServiceBusPublisher>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(specifiedPublisherName);
            var factory = provider.GetRequiredService<IAzureClientFactory<ServiceBusClient>>();
            var serviceBusClient = factory.CreateClient(serviceBusName);
            var sender = serviceBusClient.CreateSender(options.PublishTo);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ServiceBusPublisher<TMessage>>();
            var serviceBusPublisher = new ServiceBusPublisher<TMessage>(serviceBusName, specifiedPublisherName, options, sender, logger);
            return serviceBusPublisher;
        });

        services.AddSingleton<IServiceBusPublisher<TMessage>>(provider =>
        {
            var factory = provider.GetRequiredService<IServiceBusFactory>();
            var publisher = factory.GetPublisher<TMessage>(serviceBusName, specifiedPublisherName);
            return publisher;
        });

        return services.AddOptions<PublisherConfig<TMessage>>(specifiedPublisherName);
    }
}
