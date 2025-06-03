using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

public interface ITopicPublisher<in TMessage> : IServiceBusPublisher<TMessage>
    where TMessage : IMessage;
