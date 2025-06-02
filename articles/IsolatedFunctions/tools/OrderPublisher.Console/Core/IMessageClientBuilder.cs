using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Core;

public interface IMessageClientBuilder
{
    OptionsBuilder<ServiceBusPublisherConfig<TMessage>> AddPublisher<TMessage>()
        where TMessage : IMessage;
}
