using AzureServiceBusLib.Models;
using Microsoft.Extensions.Options;

namespace AzureServiceBusLib.Core;

public interface IMessageClientBuilder
{
    OptionsBuilder<TopicPublisherConfig<TMessage>> AddTopicPublisher<TMessage>()
        where TMessage : IMessage;

    OptionsBuilder<QueuePublisherConfig<TMessage>> AddQueuePublisher<TMessage>()
        where TMessage : IMessage;
}
