using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using Microsoft.Extensions.Azure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.DI;

public static class NewMessageExtensions
{
    internal const string DefaultServiceBusName = "DefaultServiceBus";

    public static OptionsBuilder<PublisherConfig<TMessage>> RegisterServiceBusPublisher<TMessage>(
        this IServiceCollection services,
        string? serviceBusName = null,
        string? publisherName = null
    )
        where TMessage : IMessage
    {
        var specifiedServiceBusName = serviceBusName ?? DefaultServiceBusName;
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

    public static IServiceCollection RegisterServiceBus(
        this IServiceCollection services,
        [Required] string connectionString,
        string? serviceBusName = null,
        Action<ServiceBusClientOptions>? options = null
    )
    {
        var specifiedServiceBusName = serviceBusName ?? DefaultServiceBusName;
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString, nameof(connectionString));

        services.TryAddSingleton<IServiceBusFactory, ServiceBusFactory>();

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
}
