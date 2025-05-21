namespace OrderPublisher.Console.Models;

internal interface ISessionMessage
{
    string SessionId { get; }
    string MessageType { get; }
}
