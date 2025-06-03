using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;
using Microsoft.Extensions.Logging;

namespace AzureServiceBusLib.Services;

internal class QueuePublisher<TMessage, TPublisherConfig>(
    ServiceBusClient serviceBusClient,
    TPublisherConfig options,
    ILogger<QueuePublisher<TMessage, TPublisherConfig>> logger
) : MessagePublisher<TMessage, TPublisherConfig>(serviceBusClient, options, logger), IQueuePublisher<TMessage>
    where TMessage : IMessage
    where TPublisherConfig : BasePublisherConfig<TMessage> { }
