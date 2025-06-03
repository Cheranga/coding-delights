using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;
using Microsoft.Extensions.Logging;

namespace AzureServiceBusLib.Services;

internal class TopicPublisher<TMessage, TPublisherConfig>(
    ServiceBusClient serviceBusClient,
    TPublisherConfig options,
    ILogger<TopicPublisher<TMessage, TPublisherConfig>> logger
) : MessagePublisher<TMessage, TPublisherConfig>(serviceBusClient, options, logger), ITopicPublisher<TMessage>
    where TMessage : IMessage
    where TPublisherConfig : BasePublisherConfig<TMessage> { }
