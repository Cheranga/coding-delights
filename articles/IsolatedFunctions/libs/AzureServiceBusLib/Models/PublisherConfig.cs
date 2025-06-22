using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;

namespace AzureServiceBusLib.Models;

public sealed record PublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required Func<ServiceBusClient> GetServiceBusClientFunc { get; set; }
    public required string PublishTo { get; set; }
    public JsonSerializerOptions? SerializerOptions { get; set; }
    public Action<TMessage, ServiceBusMessage>? MessageOptions { get; set; }
    public ServiceBusClientOptions? ServiceBusClientOptions { get; set; }
}
