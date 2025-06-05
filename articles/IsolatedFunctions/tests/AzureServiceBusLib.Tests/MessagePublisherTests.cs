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
                        _serializerOptions
                    );
                    Assert.NotNull(recMessage);
                    Assert.Single(recMessage);
                    Assert.True(recMessage[0] is { } a && data.messages.Exists(x => x.OrderId == a.OrderId));
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
                    _serializerOptions
                );

                var recMessage2 = await ReadFromQueueAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    SessionOrdersQueue,
                    data.messages[1].SessionId,
                    _serializerOptions
                );

                return (publishOperation, recMessage1, recMessage2);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And((data, operation) => operation.recMessage1 != null && data.messages[0].OrderId == operation.recMessage1[0]!.OrderId)
            .And((data, operation) => operation.recMessage2 != null && data.messages[1].OrderId == operation.recMessage2[0]!.OrderId);
    }

    [Fact(DisplayName = "Publishing to topic and receiving from both session enabled and session disabled subscriptions")]
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
            .And(data =>
            {
                var messages = _orderMessageGenerator.Generate(2);
                return (publisher: data, messages);
            })
            .Act(async data =>
            {
                var publishOperation = await data.publisher.PublishAsync(data.messages, CancellationToken.None);
                var nonSessionMessages = await ReadFromSubscriptionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersTopic,
                    JustOrdersSubscription,
                    _serializerOptions,
                    10
                );

                var sessionBasedMessages = await ReadFromSubscriptionAsSessionAsync<CreateOrderMessage>(
                    serviceBusFixture.GetConnectionString(),
                    OrdersTopic,
                    SessionBasedOrdersSubscription,
                    data.messages[0].SessionId,
                    _serializerOptions,
                    10
                );

                return (publishOperation, nonSessionMessages, sessionBasedMessage: sessionBasedMessages);
            })
            .Assert(operation => operation.publishOperation.Result is OperationResult.SuccessResult)
            .And(
                (data, operation) =>
                {
                    // There should be only one session-based message
                    Assert.Single(operation.sessionBasedMessage);

                    // Non-session subscription should receive all messages
                    var expectedOrderIds = data.messages.Select(m => m.OrderId).ToHashSet();
                    var actualOrderIds = operation.nonSessionMessages.Where(x => x != null).Select(x => x!.OrderId);
                    Assert.True(expectedOrderIds.SetEquals(actualOrderIds));
                }
            );
    }
}
