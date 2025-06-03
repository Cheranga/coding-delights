using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal class TopicPublisher<TMessage, TPublisherConfig>(
    ServiceBusClient serviceBusClient,
    TPublisherConfig options,
    ILogger<TopicPublisher<TMessage, TPublisherConfig>> logger
) : MessagePublisher<TMessage, TPublisherConfig>(serviceBusClient, options, logger), ITopicPublisher<TMessage>
    where TMessage : IMessage
    where TPublisherConfig : BasePublisherConfig<TMessage> { }
