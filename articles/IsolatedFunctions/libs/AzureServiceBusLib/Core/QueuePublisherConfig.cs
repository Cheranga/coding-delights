using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Core;

public sealed record QueuePublisherConfig<TMessage> : BasePublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string QueueName { get; set; }
    public override string PublishTo => QueueName;
}
