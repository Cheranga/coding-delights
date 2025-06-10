namespace AzureServiceBusLib.Core;

public interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
