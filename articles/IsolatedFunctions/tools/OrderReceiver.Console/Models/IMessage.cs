namespace OrderReceiver.Console.Models;

public interface IMessage
{
    string Id { get; }
    string CorrelationId { get; }
    string MessageType { get; }
}
