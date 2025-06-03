using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Core;

public sealed record TopicPublisherConfig<TMessage> : BasePublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string TopicName { get; set; }
    public override string PublishTo => TopicName;
}
