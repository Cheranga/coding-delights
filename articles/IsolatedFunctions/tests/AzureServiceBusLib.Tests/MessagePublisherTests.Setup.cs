using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Tests.Fixtures;
using AzureServiceBusLib.Tests.Models;
using Xunit.Abstractions;

namespace AzureServiceBusLib.Tests;

internal sealed record Test { }

public partial class MessagePublisherTests : IClassFixture<ServiceBusFixture>
{
    private const string JustOrdersQueue = "just-orders";
    private const string SessionOrdersQueue = "session-orders";
    private const string OrdersTopic = "sbt-orders";
    private const string JustOrdersSubscription = "just-orders";
    private const string SessionBasedOrdersSubscription = "sbts-orders";

    public MessagePublisherTests(ServiceBusFixture serviceBusFixture, ITestOutputHelper logger)
    {
        _serviceBusFixture = serviceBusFixture;
        _logger = logger;
        _test = new Test();
        _logger.WriteLine("Initialized");
    }

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AutoFaker<CreateOrderMessage> _orderMessageGenerator = new();
    private readonly ServiceBusFixture _serviceBusFixture;
    private readonly ITestOutputHelper _logger;
    private readonly Test _test;

    private static async Task<IReadOnlyList<TModel?>> ReadFromQueueAsync<TModel>(
        string serviceBusConnectionString,
        string queueName,
        JsonSerializerOptions serializerOptions,
        int numOfMessages = 1,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = serviceBusClient.CreateReceiver(
            queueName,
            new ServiceBusReceiverOptions { PrefetchCount = numOfMessages }
        );
        var messages = await receiver.ReceiveMessagesAsync(numOfMessages, TimeSpan.FromSeconds(2), token);
        return messages.Select(x => x.Body.ToObjectFromJson<TModel>(serializerOptions)).ToList();
    }

    private static async Task<IReadOnlyList<TModel?>> ReadFromSubscriptionAsync<TModel>(
        string serviceBusConnectionString,
        string topicName,
        string subscriptionName,
        JsonSerializerOptions serializerOptions,
        int numOfMessages = 1,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = serviceBusClient.CreateReceiver(
            topicName,
            subscriptionName,
            new ServiceBusReceiverOptions { PrefetchCount = numOfMessages }
        );
        var messages = await receiver.ReceiveMessagesAsync(numOfMessages, TimeSpan.FromSeconds(2), token);
        return messages.Select(x => x.Body.ToObjectFromJson<TModel>(serializerOptions)).ToList();
    }

    private static async Task<IReadOnlyList<TModel?>> ReadFromSubscriptionAsSessionAsync<TModel>(
        string serviceBusConnectionString,
        string topicName,
        string subscriptionName,
        string sessionId,
        JsonSerializerOptions serializerOptions,
        int numOfMessages = 1,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = await serviceBusClient.AcceptSessionAsync(
            topicName,
            subscriptionName,
            sessionId,
            new ServiceBusSessionReceiverOptions { PrefetchCount = numOfMessages },
            token
        );
        var messages = await receiver.ReceiveMessagesAsync(numOfMessages, TimeSpan.FromSeconds(2), token);
        return messages.Select(x => x.Body.ToObjectFromJson<TModel>(serializerOptions)).ToList();
    }

    private static async Task<IReadOnlyList<TModel?>> ReadFromQueueAsSessionAsync<TModel>(
        string serviceBusConnectionString,
        string queueName,
        string sessionId,
        JsonSerializerOptions serializerOptions,
        int numOfMessages = 1,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = await serviceBusClient.AcceptSessionAsync(
            queueName,
            sessionId,
            new ServiceBusSessionReceiverOptions { PrefetchCount = numOfMessages },
            token
        );
        var messages = await receiver.ReceiveMessagesAsync(numOfMessages, TimeSpan.FromSeconds(2), token);
        return messages.Select(x => x.Body.ToObjectFromJson<TModel>(serializerOptions)).ToList();
    }
}
