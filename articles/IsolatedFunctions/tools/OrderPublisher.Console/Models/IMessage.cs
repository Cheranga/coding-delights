namespace OrderPublisher.Console.Models;

public interface IMessage
{
    string MessageType { get; }
    string CorrelationId { get; }
}
