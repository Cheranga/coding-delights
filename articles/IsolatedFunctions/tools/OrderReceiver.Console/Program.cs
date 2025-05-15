using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderReceiver.Console;
using OrderReceiver.Console.Models;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(builder =>
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddUserSecrets<Program>()
    )
    .ConfigureServices((context, services) => services.RegisterDependencies(context.Configuration))
    .Build();

var serviceBusConfig = host.Services.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
var messageReader = host.Services.GetRequiredService<IMessageReader>();
var message = await messageReader.ReadMessageAsync<CreateOrderMessage>(
    serviceBusConfig.TopicName,
    serviceBusConfig.SubscriptionName,
    CancellationToken.None
);

Console.WriteLine($"Read order with id ${message.Id}");

var messages = await messageReader.ReadMessageBatchAsync<CreateOrderMessage>(
    serviceBusConfig.TopicName,
    serviceBusConfig.SubscriptionName,
    CancellationToken.None
);
Console.WriteLine($"Read {messages.Count} orders");
await host.RunAsync();
