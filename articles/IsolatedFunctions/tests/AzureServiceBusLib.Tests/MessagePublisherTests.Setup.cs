using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using AzureServiceBusLib.Tests.Fixtures;
using AzureServiceBusLib.Tests.Models;

namespace AzureServiceBusLib.Tests;

public partial class MessagePublisherTests : IClassFixture<ServiceBusFixture> //IAsyncLifetime
{
    private const string JustOrdersQueue = "just-orders";
    private const string SessionOrdersQueue = "session-orders";
    private const string OrdersTopic = "sbt-orders";
    private const string JustOrdersSubscription = "just-orders";
    private const string SessionBasedOrdersSubscription = "sbts-orders";

    public MessagePublisherTests(ServiceBusFixture serviceBusFixture)
    {
        _serviceBusFixture = serviceBusFixture;
    }

    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly AutoFaker<CreateOrderMessage> _orderMessageGenerator = new();
    private readonly ServiceBusFixture _serviceBusFixture;
}
