using System.Text.Json;
using Azure.Messaging.ServiceBus;
using AzureServiceBusLib.Core;
using AzureServiceBusLib.Models;

namespace AzureServiceBusLib.Tests.Helpers;

internal static class ServiceBusReaderExtensions
{
    internal static async Task<IReadOnlyList<TModel?>> ReadFromQueueAsync<TModel>(
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

    internal static async Task<IReadOnlyList<TModel?>> ReadFromSubscriptionAsync<TModel>(
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

    internal static async Task<IReadOnlyList<TModel?>> ReadFromSubscriptionAsSessionAsync<TModel>(
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

    internal static async Task<IReadOnlyList<TModel?>> ReadFromQueueAsSessionAsync<TModel>(
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
