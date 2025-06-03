namespace AzureServiceBusLib.Models;

public interface IMessage
{
    string MessageType { get; }
    string CorrelationId { get; }
}
