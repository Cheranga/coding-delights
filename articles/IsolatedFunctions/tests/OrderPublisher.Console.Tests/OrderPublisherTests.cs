using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;

namespace OrderPublisher.Console.Tests;

public class OrderPublisherTests(ServiceBusFixture serviceBusFixture) : IClassFixture<ServiceBusFixture>
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact(DisplayName = "Publish to queue and receive from queue")]
    public async Task Test1()
    {
        var connectionString = serviceBusFixture.ServiceBusContainer.GetConnectionString();

        var client = new ServiceBusClient(connectionString);
        var sender = client.CreateSender("orders");
        var receiver = client.CreateReceiver("orders");

        var msg = new AutoFaker<CreateOrderMessage>().Generate();
        await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(msg, _serializerOptions)));
        var rec = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        var recMessage = rec.Body.ToObjectFromJson<CreateOrderMessage>(_serializerOptions);
        Assert.NotNull(recMessage);
        Assert.Equal(msg.Id, recMessage.Id);
    }

    [Fact(DisplayName = "Reading from a session enabled subscription")]
    public async Task Test2()
    {
        var connectionString = serviceBusFixture.ServiceBusContainer.GetConnectionString();
        var msg = new AutoFaker<CreateOrderMessage>().Generate();

        var client = new ServiceBusClient(connectionString);
        var messagePublisher = new MessagePublisher(client, _serializerOptions);
        await messagePublisher.PublishToTopicAsync("sbt-orders", msg, CancellationToken.None);
        var receiver = await client.AcceptSessionAsync("sbt-orders", "sbts-orders", msg.OrderId.ToString());
        var rec = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
        var recMsg = rec.Body.ToObjectFromJson<CreateOrderMessage>(_serializerOptions);
        Assert.NotNull(recMsg);
        Assert.Equal(msg.Id, recMsg.Id);
    }
}
