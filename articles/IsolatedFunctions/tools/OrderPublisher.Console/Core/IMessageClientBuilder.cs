using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Core;

public interface IMessageClientBuilder
{
    OptionsBuilder<TopicPublisherConfig<TMessage>> AddTopicPublisher<TMessage>()
        where TMessage : IMessage;

    OptionsBuilder<QueuePublisherConfig<TMessage>> AddQueuePublisher<TMessage>()
        where TMessage : IMessage;
}
