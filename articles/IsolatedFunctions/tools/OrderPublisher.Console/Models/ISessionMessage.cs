using AzureServiceBusLib.Core;

namespace OrderPublisher.Console.Models;

public interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
