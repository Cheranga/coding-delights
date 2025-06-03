using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Services;

public interface IQueuePublisher<in TMessage> : IServiceBusPublisher<TMessage>
    where TMessage : IMessage;
