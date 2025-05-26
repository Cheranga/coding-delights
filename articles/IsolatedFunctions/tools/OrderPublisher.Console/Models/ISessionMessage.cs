namespace OrderPublisher.Console.Models;

internal interface ISessionMessage : IMessage
{
    string SessionId { get; }
}
