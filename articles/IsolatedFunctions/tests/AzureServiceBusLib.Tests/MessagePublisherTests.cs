using AutoBogus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.DI;
using AzureServiceBusLib.Publish;
using AzureServiceBusLib.Tests.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AzureServiceBusLib.Tests;

public partial class MessagePublisherTests
{
    [Fact(DisplayName = "Publishing to non session enabled queue and receiving")]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services.AddLogging().UseServiceBusMessageClientFactory();
                services
                    .RegisterMessagePublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IMessagePublisher<CreateOrderMessage>>();

                return publisher;
            })
            .And(data =>
            {
                var messages = _orderMessageGenerator.Generate(2);
                return (publisher: data, messages);
            })
            .Act(async data => await data.publisher.PublishAsync(data.messages, CancellationToken.None))
            .Assert(operation => operation.Result is OperationResult.SuccessResult)
            .And(
                async (data, _) =>
                {
                    var recMessage = await ReadFromQueueAsync<CreateOrderMessage>(
                        serviceBusFixture.GetConnectionString(),
                        JustOrdersQueue,
                        _serializerOptions,
                        CancellationToken.None
                    );
                    Assert.NotNull(recMessage);
                    Assert.Equal(data.messages[0].OrderId, recMessage.OrderId);
                }
            );
    }

    [Fact(DisplayName = "Publishing to session enabled queue and receiving using sessions")]
    public async Task Test2()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services.AddLogging().UseServiceBusMessageClientFactory();
                services
                    .RegisterMessagePublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.PublishTo = SessionOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IMessagePublisher<CreateOrderMessage>>();

                return publisher;
            })
            .And(data =>
            {
                var messages = _orderMessageGenerator.Generate(2);
                return (publisher: data, messages);
            })
            .Act(async data =>
            {
                var publishOperation = await data.publisher.PublishAsync(data.messages, CancellationToken.None);
                var recMessage1 = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    SessionOrdersQueue,
                    data.messages[0].SessionId,
                    _serializerOptions,
                    CancellationToken.None
                );

                var recMessage2 = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    SessionOrdersQueue,
                    data.messages[1].SessionId,
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, recMessage1, recMessage2);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And((data, operation) => operation.recMessage1 != null && data.messages[0].OrderId == operation.recMessage1.OrderId)
            .And((data, operation) => operation.recMessage2 != null && data.messages[1].OrderId == operation.recMessage2.OrderId);
    }

    [Fact(DisplayName = "Publishing to topic and receiving from non session enabled subscription")]
    public async Task Test3()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services.AddLogging().UseServiceBusMessageClientFactory();
                services
                    .RegisterMessagePublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.PublishTo = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IMessagePublisher<CreateOrderMessage>>();
                return publisher;
            })
            .Act(async data =>
            {
                var msg = new AutoFaker<CreateOrderMessage>().Generate();
                var publishOperation = await data.PublishAsync([msg], CancellationToken.None);
                var recMessage = await ReadFromSubscriptionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersTopic,
                    JustOrdersSubscription,
                    _serializerOptions,
                    CancellationToken.None
                );

                return (publishOperation, msg, recMessage);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.recMessage != null && operation.msg.OrderId == operation.recMessage.OrderId);
    }

    [Fact(DisplayName = "Publishing to topic and receiving from session enabled subscription")]
    public async Task Test4()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services.AddLogging().UseServiceBusMessageClientFactory();
                services
                    .RegisterMessagePublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = serviceBusFixture.GetConnectionString();
                        config.PublishTo = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IMessagePublisher<CreateOrderMessage>>();
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
