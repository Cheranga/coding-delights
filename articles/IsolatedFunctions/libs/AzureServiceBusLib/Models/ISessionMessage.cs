namespace AzureServiceBusLib.Models;

internal interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
