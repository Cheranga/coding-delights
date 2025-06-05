using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Tests.Fixtures;
using AzureServiceBusLib.Tests.Models;

namespace AzureServiceBusLib.Tests;

public partial class MessagePublisherTests(ServiceBusFixture serviceBusFixture) : IClassFixture<ServiceBusFixture>
{
    private const string JustOrdersQueue = "just-orders";
    private const string SessionOrdersQueue = "session-orders";
    private const string OrdersTopic = "sbt-orders";
    private const string JustOrdersSubscription = "just-orders";
    private const string SessionBasedOrdersSubscription = "sbts-orders";

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AutoFaker<CreateOrderMessage> _orderMessageGenerator = new();

    private static async Task<TModel?> ReadFromQueueAsync<TModel>(
        string serviceBusConnectionString,
        string queueName,
        JsonSerializerOptions serializerOptions,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = serviceBusClient.CreateReceiver(queueName);
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), token);
        var model = message.Body.ToObjectFromJson<TModel>(serializerOptions);
        return model;
    }

    private static async Task<TModel?> ReadFromSubscriptionAsync<TModel>(
        string serviceBusConnectionString,
        string topicName,
        string subscriptionName,
        JsonSerializerOptions serializerOptions,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = serviceBusClient.CreateReceiver(topicName, subscriptionName);
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), token);
        var model = message.Body.ToObjectFromJson<TModel>(serializerOptions);
        return model;
    }

    private static async Task<TModel?> ReadFromSubscriptionAsSessionAsync<TModel>(
        string serviceBusConnectionString,
        string topicName,
        string subscriptionName,
        string sessionId,
        JsonSerializerOptions serializerOptions,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = await serviceBusClient.AcceptSessionAsync(
            topicName,
            subscriptionName,
            sessionId,
            cancellationToken: token
        );
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), token);
        var model = message.Body.ToObjectFromJson<TModel>(serializerOptions);
        return model;
    }

    private static async Task<TModel?> ReadFromQueueAsSessionAsync<TModel>(
        string serviceBusConnectionString,
        string queueName,
        string sessionId,
        JsonSerializerOptions serializerOptions,
        CancellationToken token = default
    )
        where TModel : IMessage
    {
        await using var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        await using var receiver = await serviceBusClient.AcceptSessionAsync(queueName, sessionId, cancellationToken: token);
        var message = await receiver.ReceiveMessageAsync(TimeSpan.FromSeconds(2), token);
        var model = message.Body.ToObjectFromJson<TModel>(serializerOptions);
        return model;
    }
}
