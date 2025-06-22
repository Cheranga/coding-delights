namespace OrderPublisher.Console.Models;

internal sealed record CreateOrderMessage : ISessionMessage
{
    public required Guid OrderId { get; init; }
    public required Guid ReferenceId { get; init; }
    public required DateTimeOffset OrderDate { get; init; }

    public required IReadOnlyCollection<OrderItem> Items { get; init; }
    public string SessionId => OrderId.ToString();
    public string CorrelationId => ReferenceId.ToString();
    public string MessageType => nameof(CreateOrderMessage);
}
