using AzureServiceBusLib.Core;
using AzureServiceBusLib.DI;
using AzureServiceBusLib.Publish;
using AzureServiceBusLib.Tests.Models;
using Microsoft.Extensions.DependencyInjection;

namespace AzureServiceBusLib.Tests;

public partial class MessagePublisherTests
{
    [Fact(DisplayName = "Publishing to non session enabled queue using a typed publisher and receiving")]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection().AddLogging().RegisterServiceBus(_serviceBusFixture.GetConnectionString());

                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>(publisherName: "another-publisher")
                    .Configure(config =>
                    {
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IServiceBusPublisher<CreateOrderMessage>>();

                var anotherPublisher = serviceProvider
                    .GetRequiredService<IServiceBusFactory>()
                    .GetPublisher<CreateOrderMessage>("another-publisher");

                return (publisher, anotherPublisher);
            })
            .And(data =>
            {
                var messages = _orderMessageGenerator.Generate(2);
                return (data.publisher, data.anotherPublisher, messages);
            })
            .Act(async data =>
            {
                var operation1 = await data.publisher.PublishAsync(data.messages, CancellationToken.None);
                var operation2 = await data.anotherPublisher.PublishAsync(data.messages, CancellationToken.None);

                return (operation1, operation2);
            })
            .Assert(result => result.operation1.Result is OperationResult.SuccessResult)
            .And(result => result.operation2.Result is OperationResult.SuccessResult)
            .And(
                async (data, _) =>
                {
                    var recMessages = await ReadFromQueueAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        JustOrdersQueue,
                        _serializerOptions,
                        10
                    );

                    Assert.DoesNotContain(recMessages, x => x == null);
                    var receivedOrders = recMessages.Select(x => x!.OrderId).ToHashSet();
                    var expectedOrders = data.messages.Select(x => x.OrderId).ToHashSet();
                    Assert.True(expectedOrders.SetEquals(receivedOrders));
                }
            );
    }

    [Fact(DisplayName = "Publishing to session enabled queue using typed client through ServiceBusFactory and receiving using sessions")]
    public async Task Test2()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services
                    .AddLogging()
                    .RegisterServiceBus(_serviceBusFixture.GetConnectionString())
                    .RegisterServiceBusPublisher<CreateOrderMessage>(publisherName: "orders")
                    .Configure(config =>
                    {
                        config.PublishTo = SessionOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetRequiredService<IServiceBusFactory>();
                var publisher = factory.GetPublisher<CreateOrderMessage>("orders");

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
                return publishOperation;
            })
            .Assert(operation => operation.Result is OperationResult.SuccessResult)
            .And(
                async (data, _) =>
                {
                    var sessionId = data.messages[0].SessionId;
                    var recMessage = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        SessionOrdersQueue,
                        sessionId,
                        _serializerOptions
                    );

                    Assert.True(recMessage != null && data.messages[0].OrderId == recMessage[0]!.OrderId);
                }
            )
            .And(
                async (data, _) =>
                {
                    var sessionId = data.messages[1].SessionId;
                    var recMessage = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        SessionOrdersQueue,
                        sessionId,
                        _serializerOptions
                    );

                    Assert.True(recMessage != null && data.messages[1].OrderId == recMessage[0]!.OrderId);
                }
            );
    }

    [Fact(
        DisplayName = "Publishing to topic using named client through ServiceBusFactory and receiving from both session enabled and session disabled subscriptions"
    )]
    public async Task Test3()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services
                    .AddLogging()
                    .RegisterServiceBus(_serviceBusFixture.GetConnectionString(), "orders")
                    .RegisterServiceBusPublisher<CreateOrderMessage>("orders", "orders-publisher")
                    .Configure(config =>
                    {
                        config.PublishTo = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetRequiredService<IServiceBusFactory>();
                var publisher = factory.GetPublisher<CreateOrderMessage>("orders", "orders-publisher");
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
                return publishOperation;
            })
            .Assert(operation => operation.Result is OperationResult.SuccessResult)
            .And(
                async (data, _) =>
                {
                    var sessionBasedMessages = await ReadFromSubscriptionAsSessionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        OrdersTopic,
                        SessionBasedOrdersSubscription,
                        data.messages[0].SessionId,
                        _serializerOptions,
                        10
                    );

                    // There should be only one session-based message
                    Assert.Single(sessionBasedMessages);
                }
            )
            .And(
                async (data, _) =>
                {
                    var nonSessionMessages = await ReadFromSubscriptionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        OrdersTopic,
                        JustOrdersSubscription,
                        _serializerOptions,
                        10
                    );

                    // Non-session subscription should receive all messages
                    var expectedOrderIds = data.messages.Select(m => m.OrderId).ToHashSet();
                    var actualOrderIds = nonSessionMessages.Where(x => x != null).Select(x => x!.OrderId);
                    Assert.True(expectedOrderIds.SetEquals(actualOrderIds));
                }
            );
    }
}
