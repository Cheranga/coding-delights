using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using OrderPublisher.Console.Models;
using OrderPublisher.Console.Services;

namespace OrderPublisher.Console;

internal static class Bootstrapper
{
    private static IServiceCollection RegisterApplicationServices(this IServiceCollection services)
    {
        services.AddSingleton(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false,
            }
        );
        services.TryAddSingleton<IOrderGenerator<CreateOrderMessage>, OrderGenerator>();
        services.AddHostedService<OrderPublisherBackgroundService>();
        return services;
    }

    private static IServiceCollection RegisterConfigurations(this IServiceCollection services)
    {
        services.AddOptions<ServiceBusConfig>().BindConfiguration(nameof(ServiceBusConfig));
        return services;
    }

    private static IServiceCollection RegisterMessagingServices(this IServiceCollection services)
    {
        //
        // Registering the ServiceBusClient with the connection string from the configuration
        //
        services
            .AddSingleton(sp =>
            {
                var config = sp.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
                return new ServiceBusClient(config.ConnectionString);
            })
            // Registering the `ServiceBusClient` as an `IAsyncDisposable` to ensure it is disposed of correctly
            .AddSingleton<IAsyncDisposable>(sp => sp.GetRequiredService<ServiceBusClient>());

        //
        // Registering the message publisher for the CreateOrderMessage
        //
        services
            .RegisterMessageClientBuilder()
            .AddTopicPublisher<CreateOrderMessage>()
            .Configure<IOptions<ServiceBusConfig>>(
                (config, busConfigOptions) =>
                {
                    var busConfig = busConfigOptions.Value;
                    config.ConnectionString = busConfig.ConnectionString;
                    config.TopicName = busConfig.TopicName;
                    config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.OrderId.ToString();
                }
            );

        //
        // Registering the OrderGenerator for CreateOrderMessage
        //
        services.TryAddSingleton<IOrderGenerator<CreateOrderMessage>, OrderGenerator>();
        return services;
    }

    public static void RegisterDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterConfigurations().RegisterMessagingServices().RegisterApplicationServices();
    }
}
