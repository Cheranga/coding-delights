using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Services;

public interface IQueuePublisher<in TMessage> : IServiceBusPublisher<TMessage>
    where TMessage : IMessage;
