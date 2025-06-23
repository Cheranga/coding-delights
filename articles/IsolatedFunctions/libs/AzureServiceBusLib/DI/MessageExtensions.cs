using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;
using AzureServiceBusLib.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.DI;

/// <summary>
/// Provides extension methods for registering Azure Service Bus messaging components in the dependency injection container.
/// </summary>
public static class MessageExtensions
{
    /// <summary>
    /// Registers a Service Bus publisher for a specific message type in the dependency injection container.
    /// The method registers both generic and non-generic publisher interfaces and configures the necessary dependencies.
    /// </summary>
    /// <typeparam name="TMessage">The type of message to be published, must implement IMessage interface.</typeparam>
    /// <param name="services">The IServiceCollection to add the publisher services to.</param>
    /// <param name="publisherName">Optional name for the publisher configuration. If not specified, the message type name is used.</param>
    /// <returns>An OptionsBuilder instance for configuring the publisher options.</returns>
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

            var serviceBusClient = options.GetServiceBusClientFunc();
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

    /// <summary>
    /// Registers the Azure Service Bus factory in the dependency injection container.
    /// Must be called before registering any publishers or subscribers.
    /// </summary>
    /// <param name="services">The IServiceCollection to add the Service Bus factory to.</param>
    /// <returns>The IServiceCollection instance to enable method chaining.</returns>
    public static IServiceCollection RegisterServiceBus(this IServiceCollection services)
    {
        services.AddSingleton<IServiceBusFactory, ServiceBusFactory>();

        return services;
    }
}
