using AzureServiceBusLib.Core;
using AzureServiceBusLib.Publish;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.DI;

public static class MessageExtensions
{
    public static IServiceCollection UseServiceBusMessageClientFactory(this IServiceCollection services)
    {
        services.AddSingleton<IMessagePublisherFactory, MessagePublisherFactory>();
        return services;
    }

    public static OptionsBuilder<PublisherConfig<TMessage>> RegisterMessagePublisher<TMessage>(
        this IServiceCollection services,
        string? publisherName = null
    )
        where TMessage : IMessage
    {
        publisherName ??= typeof(TMessage).Name;

        services.AddSingleton<IMessagePublisher>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<PublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(publisherName);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<MessagePublisher<TMessage>>();

            var publisher = new MessagePublisher<TMessage>(publisherName, options, logger);
            return publisher;
        });

        services.AddSingleton<IMessagePublisher<TMessage>>(provider =>
        {
            var factory = provider.GetRequiredService<IMessagePublisherFactory>();
            var publisher = factory.GetPublisher<TMessage>(publisherName);
            return publisher;
        });

        return services.AddOptions<PublisherConfig<TMessage>>(publisherName);
    }
}
