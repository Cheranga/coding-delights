using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

public interface ITopicPublisher<in TMessage> : IServiceBusPublisher<TMessage>
    where TMessage : IMessage;
