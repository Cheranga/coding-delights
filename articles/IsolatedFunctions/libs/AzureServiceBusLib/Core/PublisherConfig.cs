using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Core;

public sealed record PublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string ConnectionString { get; set; }
    public required string PublishTo { get; set; }
    public JsonSerializerOptions? SerializerOptions { get; set; }
    public Action<TMessage, ServiceBusMessage>? MessageOptions { get; set; }
}
