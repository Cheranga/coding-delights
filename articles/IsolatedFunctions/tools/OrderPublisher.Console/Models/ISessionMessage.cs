using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace OrderPublisher.Console.Models;

public interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
