using System.Text.Json;
using Azure.Messaging.ServiceBus;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Core;

public sealed record QueuePublisherConfig<TMessage> : BasePublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string QueueName { get; set; }
    public override string PublishTo => QueueName;
}
