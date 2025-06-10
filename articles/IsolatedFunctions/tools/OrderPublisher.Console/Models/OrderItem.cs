namespace OrderPublisher.Console.Models;

internal record OrderItem
{
    public required string ProductId { get; init; }
    public required decimal Quantity { get; init; }
    public required decimal Price { get; init; }
    public required string Metric { get; init; }
}
