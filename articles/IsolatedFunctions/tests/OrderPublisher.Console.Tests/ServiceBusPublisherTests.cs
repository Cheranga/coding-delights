using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Microsoft.Extensions.DependencyInjection;
using OrderPublisher.Console.Core;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;

namespace OrderPublisher.Console.Tests;

public partial class ServiceBusPublisherTests(ServiceBusFixture serviceBusFixture) : IClassFixture<ServiceBusFixture>
{
    private const string OrdersQueue = "orders";
    private const string OrdersTopic = "sbt-orders";
    private const string SessionBasedOrdersSubscription = "sbts-orders";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact(DisplayName = "Publishing to queue and receiving from queue")]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services
                    .AddLogging()
                    .RegisterMessageClientBuilder()
                    .AddTopicPublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.TopicOrQueueName = OrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IServiceBusPublisher<CreateOrderMessage>>();

                return publisher;
            })
            .Act(async data =>
            {
                var msg = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync(msg, CancellationToken.None);
                var recMessage = await ReadFromQueueAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersQueue,
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, msg, recMessage);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.recMessage != null && operation.msg.OrderId == operation.recMessage.OrderId);
    }

    [Fact(DisplayName = "Publishing to topic with a session id")]
    public async Task Test2()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services
                    .AddLogging()
                    .RegisterMessageClientBuilder()
                    .AddTopicPublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.TopicOrQueueName = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IServiceBusPublisher<CreateOrderMessage>>();
                return publisher;
            })
            .Act(async data =>
            {
                var msg = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync(msg, CancellationToken.None);
                var recMessage = await ReadFromSubscriptionAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersTopic,
                    SessionBasedOrdersSubscription,
                    msg.OrderId.ToString(),
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, msg, recMessage);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.recMessage != null && operation.msg.OrderId == operation.recMessage.OrderId);
    }
}
