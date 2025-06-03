namespace AzureServiceBusLib.Models;

public interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
