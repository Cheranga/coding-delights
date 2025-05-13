namespace OrderPublisher.Console.Models;

public sealed record CreateOrderMessage : IMessage
{
    public required Guid OrderId { get; set; }
    public required Guid ReferenceId { get; set; }
    public required DateTimeOffset OrderDate { get; set; }

    public required IReadOnlyCollection<OrderItem> Items { get; set; }
    public string Id => OrderId.ToString();
    public string CorrelationId => ReferenceId.ToString();
    public string MessageType => nameof(CreateOrderMessage);
}
