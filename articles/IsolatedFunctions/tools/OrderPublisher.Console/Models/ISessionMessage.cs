using ShotCaller.Azure.ServiceBus.Messaging.Core;

namespace OrderPublisher.Console.Models;

public interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
