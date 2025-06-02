using System.Diagnostics.CodeAnalysis;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;

namespace OrderPublisher.Console.Core;

[SuppressMessage("ReSharper", "UnusedVariable")]
[SuppressMessage("Minor Code Smell", "S1481:Unused local variables should be removed")]
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
        //
        // Let's do the HttpClientFactory like approach later
        //
        var publisherName = typeof(TMessage).Name;
        _services.TryAddSingleton<IServiceBusPublisher<TMessage>>(provider =>
        {
            var optionsMonitor = provider.GetRequiredService<IOptionsMonitor<TopicPublisherConfig<TMessage>>>();
            var options = optionsMonitor.Get(publisherName);
            var serviceBusClient = new ServiceBusClient(options.ConnectionString);
            var logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger<ServiceBusPublisher<TMessage>>();

            var publisher = new ServiceBusPublisher<TMessage>(serviceBusClient, options, logger);
            return publisher;
        });
        return _services.AddOptions<TopicPublisherConfig<TMessage>>(publisherName);
    }
}
