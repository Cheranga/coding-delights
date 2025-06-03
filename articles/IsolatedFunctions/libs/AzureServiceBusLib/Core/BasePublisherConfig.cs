using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Core;

public abstract record BasePublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string ConnectionString { get; set; }
    public JsonSerializerOptions? SerializerOptions { get; set; }
    public Action<TMessage, ServiceBusMessage>? MessageOptions { get; set; }

    public abstract string PublishTo { get; }
}
