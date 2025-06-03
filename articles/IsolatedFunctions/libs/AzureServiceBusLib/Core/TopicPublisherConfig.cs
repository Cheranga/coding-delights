using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Core;

public sealed record TopicPublisherConfig<TMessage> : BasePublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string TopicName { get; set; }
    public override string PublishTo => TopicName;
}
