using AzureServiceBusLib.Core;
using AzureServiceBusLib.DI;
using AzureServiceBusLib.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using static AzureServiceBusLib.Tests.Helpers.ServiceBusReaderExtensions;

namespace AzureServiceBusLib.Tests;

public partial class MessagePublisherTests
{
    [Fact(DisplayName = "Using both typed and named publishers to publish messages to a queue")]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var connectionString = _serviceBusFixture.GetConnectionString();
                var services = new ServiceCollection().AddLogging().RegisterServiceBus();

                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = connectionString;
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>("another-publisher")
                    .Configure(config =>
                    {
                        config.ConnectionString = connectionString;
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var typedPublisher = serviceProvider.GetRequiredService<IServiceBusPublisher<CreateOrderMessage>>();

                var namedPublisher = serviceProvider
                    .GetRequiredService<IServiceBusFactory>()
                    .GetPublisher<CreateOrderMessage>("another-publisher");

                return (typedPublisher, namedPublisher);
            })
            .And(data =>
            {
                var messages = _orderMessageGenerator.Generate(2);
                return (data.typedPublisher, data.namedPublisher, messages);
            })
            .Act(async data =>
            {
                var typedOperation = await data.typedPublisher.PublishAsync(data.messages, CancellationToken.None);
                var namedOperation = await data.namedPublisher.PublishAsync(data.messages, CancellationToken.None);

                return (typedOperation, namedOperation);
            })
            .Assert(result => result.typedOperation.Result is OperationResult.SuccessResult)
            .And(result => result.namedOperation.Result is OperationResult.SuccessResult)
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

    [Fact(DisplayName = "Using both typed and named publishers to publish messages to a session enabled queue")]
    public async Task Test2()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection().AddLogging().RegisterServiceBus();

                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = _serviceBusFixture.GetConnectionString();
                        config.PublishTo = SessionOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });
                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>("orders")
                    .Configure(config =>
                    {
                        config.ConnectionString = _serviceBusFixture.GetConnectionString();
                        config.PublishTo = SessionOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetRequiredService<IServiceBusFactory>();
                var typedPublisher = serviceProvider.GetRequiredService<IServiceBusPublisher<CreateOrderMessage>>();
                var namedPublisher = factory.GetPublisher<CreateOrderMessage>("orders");

                return (typedPublisher, namedPublisher);
            })
            .And(data =>
            {
                var messages = _orderMessageGenerator.Generate(2);
                return (data.typedPublisher, data.namedPublisher, messages);
            })
            .Act(async data =>
            {
                var typedOperation = await data.typedPublisher.PublishAsync(data.messages, CancellationToken.None);
                var namedOperation = await data.namedPublisher.PublishAsync(data.messages, CancellationToken.None);
                return (typedOperation, namedOperation);
            })
            .Assert(operation => operation.typedOperation.Result is OperationResult.SuccessResult)
            .And(operation => operation.namedOperation.Result is OperationResult.SuccessResult)
            .And(
                async (data, _) =>
                {
                    var sessionId = data.messages[0].SessionId;
                    var recMessages = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        SessionOrdersQueue,
                        sessionId,
                        _serializerOptions,
                        10
                    );

                    Assert.Equal(2, recMessages.Count);
                }
            )
            .And(
                async (data, _) =>
                {
                    var sessionId = data.messages[1].SessionId;
                    var recMessages = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        SessionOrdersQueue,
                        sessionId,
                        _serializerOptions,
                        10
                    );

                    Assert.Equal(2, recMessages.Count);
                }
            );
    }

    [Fact(DisplayName = "Publishing to topic using a typed publisher and receiving from subscribers")]
    public async Task Test3()
    {
        await Arrange(() =>
            {
                var services = new ServiceCollection();
                services
                    .AddLogging()
                    .RegisterServiceBus()
                    .RegisterServiceBusPublisher<CreateOrderMessage>()
                    .Configure(config =>
                    {
                        config.ConnectionString = _serviceBusFixture.GetConnectionString();
                        config.PublishTo = OrdersTopic;
                        config.SerializerOptions = _serializerOptions;
                        config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.SessionId;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var publisher = serviceProvider.GetRequiredService<IServiceBusFactory>().GetPublisher<CreateOrderMessage>();
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
                    //
                    // Read from a session-based subscription
                    //
                    var sessionBasedMessages = await ReadFromSubscriptionAsSessionAsync<CreateOrderMessage>(
                        _serviceBusFixture.GetConnectionString(),
                        OrdersTopic,
                        SessionBasedOrdersSubscription,
                        data.messages[0].SessionId,
                        _serializerOptions,
                        10
                    );

                    Assert.Single(sessionBasedMessages);
                }
            )
            .And(
                async (data, _) =>
                {
                    //
                    // Read from a non-session subscription
                    //
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
