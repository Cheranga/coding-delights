using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Logging;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

internal class QueuePublisher<TMessage, TPublisherConfig>(
    ServiceBusClient serviceBusClient,
    TPublisherConfig options,
    ILogger<QueuePublisher<TMessage, TPublisherConfig>> logger
) : MessagePublisher<TMessage, TPublisherConfig>(serviceBusClient, options, logger), IQueuePublisher<TMessage>
    where TMessage : IMessage
    where TPublisherConfig : BasePublisherConfig<TMessage> { }
