using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.DI;
using AzureServiceBusLib.Tests.Fixtures;
using AzureServiceBusLib.Tests.Models;
using Microsoft.Extensions.DependencyInjection;
using static AzureServiceBusLib.Tests.Helpers.ServiceBusReaderExtensions;

namespace AzureServiceBusLib.Tests;

internal sealed record OrderCreatedEvent(Guid OrderId, DateTimeOffset CreatedAt) : IMessage
{
    public string MessageType => nameof(OrderCreatedEvent);
    public string CorrelationId => OrderId.ToString();
}

public class MultiServiceBusTests : IAsyncLifetime
{
    private const string JustOrdersQueue = "just-orders";

    private readonly ServiceBusFixture _serviceBusFixture1;
    private readonly ServiceBusFixture _serviceBusFixture2;

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AutoFaker<CreateOrderMessage> _orderMessageGenerator = new();
    private readonly AutoFaker<OrderCreatedEvent> _orderCreatedEventGenerator = new();

    public MultiServiceBusTests()
    {
        _serviceBusFixture1 = new ServiceBusFixture();
        _serviceBusFixture2 = new ServiceBusFixture();
    }

    [Fact(DisplayName = "Using typed publishers with multiple service buses")]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var connectionString1 = _serviceBusFixture1.GetConnectionString();
                var connectionString2 = _serviceBusFixture2.GetConnectionString();

                var services = new ServiceCollection().AddLogging().RegisterServiceBus();

                services
                    .RegisterServiceBusPublisher<CreateOrderMessage>("A")
                    .Configure(config =>
                    {
                        config.ConnectionString = connectionString1;
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                services
                    .RegisterServiceBusPublisher<OrderCreatedEvent>("B")
                    .Configure(config =>
                    {
                        config.ConnectionString = connectionString2;
                        config.PublishTo = JustOrdersQueue;
                        config.SerializerOptions = _serializerOptions;
                    });

                var serviceProvider = services.BuildServiceProvider();
                var factory = serviceProvider.GetRequiredService<IServiceBusFactory>();
                var publisherA = factory.GetPublisher<CreateOrderMessage>("A");
                var publisherB = factory.GetPublisher<OrderCreatedEvent>("B");

                return (publisherA, publisherB);
            })
            .And(data =>
            {
                var createOrderMessages = _orderMessageGenerator.Generate(3);
                var orderCreatedEvents = _orderCreatedEventGenerator.Generate(3);
                return (data.publisherA, data.publisherB, data, createOrderMessages, orderCreatedEvents);
            })
            .Act(async data =>
            {
                var operationA = await data.publisherA.PublishAsync(data.createOrderMessages, CancellationToken.None);
                var operationB = await data.publisherB.PublishAsync(data.orderCreatedEvents, CancellationToken.None);

                return (operationA, operationB);
            })
            .Assert(result => result.operationA.Result is OperationResult.SuccessResult)
            .And(result => result.operationB.Result is OperationResult.SuccessResult)
            .And(
                async (data, _) =>
                {
                    var recMessages = await ReadFromQueueAsync<CreateOrderMessage>(
                        _serviceBusFixture1.GetConnectionString(),
                        JustOrdersQueue,
                        _serializerOptions,
                        10
                    );

                    Assert.DoesNotContain(recMessages, x => x == null);
                    var receivedOrders = recMessages.Select(x => x!.OrderId).ToHashSet();
                    var expectedOrders = data.createOrderMessages.Select(x => x.OrderId).ToHashSet();
                    Assert.True(expectedOrders.SetEquals(receivedOrders));
                }
            )
            .And(
                async (data, _) =>
                {
                    var recMessages = await ReadFromQueueAsync<OrderCreatedEvent>(
                        _serviceBusFixture2.GetConnectionString(),
                        JustOrdersQueue,
                        _serializerOptions,
                        10
                    );

                    Assert.DoesNotContain(recMessages, x => x == null);
                    var receivedOrders = recMessages.Select(x => x!.OrderId).ToHashSet();
                    var expectedOrders = data.orderCreatedEvents.Select(x => x.OrderId).ToHashSet();
                    Assert.True(expectedOrders.SetEquals(receivedOrders));
                }
            );
    }

    public async Task InitializeAsync()
    {
        await _serviceBusFixture1.InitializeAsync();
        await _serviceBusFixture2.InitializeAsync();
    }

    public async Task DisposeAsync()
    {
        await _serviceBusFixture1.DisposeAsync();
        await _serviceBusFixture2.DisposeAsync();
    }
}
