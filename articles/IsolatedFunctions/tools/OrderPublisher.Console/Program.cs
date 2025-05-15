using AutoBogus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderPublisher.Console;
using OrderPublisher.Console.Models;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(builder =>
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddUserSecrets<Program>()
    )
    .ConfigureServices((context, services) => services.RegisterDependencies(context.Configuration))
    .Build();

var serviceBusConfig = host.Services.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
var messagePublisher = host.Services.GetRequiredService<IMessagePublisher>();
var orderId = Guid.NewGuid();
var orders = new AutoFaker<CreateOrderMessage>().RuleFor(x => x.OrderId, orderId).Generate(5);
await messagePublisher.PublishToTopicAsync(serviceBusConfig.TopicName, orders, CancellationToken.None);

Console.WriteLine("Messages published to topic");
await host.RunAsync();
