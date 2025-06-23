namespace AzureServiceBusLib.Core;

/// <summary>
/// Defines the contract for messages that can be published through the Azure Service Bus system.
/// This interface serves as a base contract for all message types that need to be processed
/// through the service bus publisher infrastructure.
/// </summary>
public interface IMessage
{
    /// <summary>
    /// Gets the message type identifier used for publisher registration and message routing.
    /// If not explicitly specified during registration, the default value is the name of
    /// the implementing class.
    /// </summary>
    string MessageType { get; }

    /// <summary>
    /// Gets the correlation identifier used for message tracking and correlation.
    /// This identifier helps in tracking related messages and associating them
    /// with their corresponding operations in the system.
    /// </summary>
    string CorrelationId { get; }
}
