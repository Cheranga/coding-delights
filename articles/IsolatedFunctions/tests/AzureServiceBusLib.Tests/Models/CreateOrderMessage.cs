using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Tests.Models;

public sealed record CreateOrderMessage : ISessionMessage
{
    public required Guid OrderId { get; set; }
    public required Guid ReferenceId { get; set; }
    public required DateTimeOffset OrderDate { get; set; }

    public required IReadOnlyCollection<OrderItem> Items { get; set; }
    public string SessionId => OrderId.ToString();
    public string CorrelationId => ReferenceId.ToString();
    public string MessageType => nameof(CreateOrderMessage);
}
