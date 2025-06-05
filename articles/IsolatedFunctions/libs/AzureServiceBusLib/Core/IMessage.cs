namespace AzureServiceBusLib.Core;

public interface IMessage
{
    string MessageType { get; }
    string CorrelationId { get; }
}
