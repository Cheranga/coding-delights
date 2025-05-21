using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Azure.Messaging.ServiceBus;
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
    public async Task Test1() =>
        await Arrange(() =>
            {
                var connectionString = serviceBusFixture.GetConnectionString();

                var clientFunc = () => new ServiceBusClient(connectionString);
                var senderFunc = (ServiceBusClient client) => client.CreateSender("orders");
                var receiverFunc = (ServiceBusClient client) => client.CreateReceiver("orders");

                var msg = new AutoFaker<CreateOrderMessage>().Generate();

                return (clientFunc, senderFunc, receiverFunc, msg);
            })
            .Act(async data =>
            {
                await using var client = data.clientFunc();
                await using var sender = data.senderFunc(client);
                await sender.SendMessageAsync(new ServiceBusMessage(JsonSerializer.Serialize(data.msg, _serializerOptions)));
                await using var receiver = data.receiverFunc(client);
                var rec = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
                var recMessage = rec.Body.ToObjectFromJson<CreateOrderMessage>(_serializerOptions);

                return recMessage;
            })
            .Assert((data, result) => result != null && data.msg.Id == result.Id);

    [Fact(DisplayName = "Reading from a session enabled subscription")]
    public async Task Test2() =>
        await Arrange(() =>
            {
                var connectionString = serviceBusFixture.GetConnectionString();

                var clientFunc = () => new ServiceBusClient(connectionString);
                var messagePublisherFunc = (ServiceBusClient client, JsonSerializerOptions options) =>
                    new MessagePublisher(client, options);
                var receiverFunc = (ServiceBusClient client) => client.CreateReceiver("orders");

                var msg = new AutoFaker<CreateOrderMessage>().Generate();

                return (clientFunc, messagePublisherFunc, receiverFunc, msg);
            })
            .Act(async data =>
            {
                await using var client = data.clientFunc();
                var publisher = data.messagePublisherFunc(client, _serializerOptions);
                await publisher.PublishToTopicAsync("sbt-orders", data.msg, CancellationToken.None);
                await using var receiver = await client.AcceptSessionAsync("sbt-orders", "sbts-orders", data.msg.OrderId.ToString());
                var rec = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2));
                var recMessage = rec.Body.ToObjectFromJson<CreateOrderMessage>(_serializerOptions);

                return recMessage;
            })
            .Assert((data, result) => result != null && data.msg.Id == result.Id);
}
