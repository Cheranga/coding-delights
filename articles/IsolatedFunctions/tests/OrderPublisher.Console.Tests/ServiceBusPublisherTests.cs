using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Services;
using Microsoft.Extensions.DependencyInjection;
using OrderPublisher.Console.Models;

namespace OrderPublisher.Console.Tests;

public partial class ServiceBusPublisherTests(ServiceBusFixture serviceBusFixture) : IClassFixture<ServiceBusFixture>
{
    private const string OrdersQueue = "just-orders";
    private const string SessionOrdersQueue = "session-orders";
    private const string OrdersTopic = "sbt-orders";
    private const string OrdersSubscription = "just-orders";
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
                    .AddQueuePublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.QueueName = OrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IQueuePublisher<CreateOrderMessage>>();

                return publisher;
            })
            .Act(async data =>
            {
                var msg1 = new AutoFaker<CreateOrderMessage>().Generate();
                var msg2 = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync([msg1, msg2], CancellationToken.None);
                var recMessage = await ReadFromQueueAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersQueue,
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, msg: msg1, recMessage);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.recMessage != null && operation.msg.OrderId == operation.recMessage.OrderId);
    }

    [Fact(DisplayName = "Publishing to session enabled queue and reading using session ids")]
    public async Task Test2()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services
                    .AddLogging()
                    .RegisterMessageClientBuilder()
                    .AddQueuePublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.QueueName = SessionOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IQueuePublisher<CreateOrderMessage>>();

                return publisher;
            })
            .Act(async data =>
            {
                var msg1 = new AutoFaker<CreateOrderMessage>().Generate();
                var msg2 = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync([msg1, msg2], CancellationToken.None);
                var recMessage1 = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    SessionOrdersQueue,
                    msg1.SessionId,
                    _serializerOptions,
                    CancellationToken.None
                );

                var recMessage2 = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    SessionOrdersQueue,
                    msg2.SessionId,
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, msg1, msg2, recMessage1, recMessage2);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.recMessage1 != null && operation.msg1.OrderId == operation.recMessage1.OrderId)
            .And(operation => operation.recMessage2 != null && operation.msg2.OrderId == operation.recMessage2.OrderId);
    }

    [Fact(DisplayName = "Publishing to topic and receiving from subscription")]
    public async Task Test3()
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
                        config.TopicName = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<ITopicPublisher<CreateOrderMessage>>();
                return publisher;
            })
            .Act(async data =>
            {
                var msg = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync([msg], CancellationToken.None);
                var recMessage = await ReadFromSubscriptionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersTopic,
                    OrdersSubscription,
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, msg, recMessage);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.recMessage != null && operation.msg.OrderId == operation.recMessage.OrderId);
    }

    [Fact(DisplayName = "Publishing to topic with a session id")]
    public async Task Test4()
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
                        config.TopicName = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<ITopicPublisher<CreateOrderMessage>>();
                return publisher;
            })
            .Act(async data =>
            {
                var msg = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync([msg], CancellationToken.None);
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
