using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Tests.Models;

internal sealed record OrderCreatedEvent(Guid OrderId, DateTimeOffset CreatedAt) : IMessage
{
    public string MessageType => nameof(OrderCreatedEvent);
    public string CorrelationId => OrderId.ToString();
}
