using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Models;

/// <summary>
/// Represents the configuration settings for an Azure Service Bus publisher.
/// This configuration is used to customize how messages are published to Service Bus topics or queues.
/// </summary>
/// <typeparam name="TMessage">The type of message to be published, must implement IMessage interface.</typeparam>
public sealed record PublisherConfig<TMessage>
    where TMessage : IMessage
{
    /// <summary>
    /// Gets or sets the function that creates a new instance of ServiceBusClient.
    /// This is required to establish connection with Azure Service Bus.
    /// </summary>
    public required Func<ServiceBusClient> GetServiceBusClientFunc { get; set; }

    /// <summary>
    /// Gets or sets the name of the topic or queue to publish messages to.
    /// This is required to specify the destination for messages.
    /// </summary>
    public required string PublishTo { get; set; }

    /// <summary>
    /// Gets or sets the JSON serializer options used when serializing messages.
    /// If not specified, default serialization options will be used.
    /// </summary>
    public JsonSerializerOptions? SerializerOptions { get; set; }

    /// <summary>
    /// Gets or sets the action to configure additional ServiceBusMessage options before publishing.
    /// This can be used to set message properties, session ID, or other message-specific settings.
    /// </summary>
    public Action<TMessage, ServiceBusMessage>? MessageOptions { get; set; }

    /// <summary>
    /// Gets or sets the options for configuring the ServiceBusClient.
    /// If not specified, default client options will be used.
    /// </summary>
    public ServiceBusClientOptions? ServiceBusClientOptions { get; set; }
}
