using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Core;

public sealed class ServiceBusPublisherConfig<TMessage>
    where TMessage : IMessage
{
    public required string TopicOrQueueName { get; set; }
    public required string ConnectionString { get; set; }

    public JsonSerializerOptions SerializerOptions { get; set; } =
        new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

    public Action<TMessage, ServiceBusMessage>? MessageOptions { get; set; }
}
