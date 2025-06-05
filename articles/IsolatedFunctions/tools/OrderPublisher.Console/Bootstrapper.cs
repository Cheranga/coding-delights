using System.Text.Json;
using System.Text.Json.Serialization;
using AzureServiceBusLib.Services;
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

    private static IServiceCollection RegisterMessagingServices(
        this IServiceCollection services,
        string serviceBusConfigSectionName = nameof(ServiceBusConfig)
    )
    {
        services.AddOptions<ServiceBusConfig>().BindConfiguration(serviceBusConfigSectionName);
        //
        // Registering the message publisher for the CreateOrderMessage
        //
        services.UseServiceBusMessageClientFactory();
        services
            .RegisterServiceBusMessagePublisher<CreateOrderMessage>()
            .Configure<IOptions<ServiceBusConfig>>(
                (config, busConfigOptions) =>
                {
                    var busConfig = busConfigOptions.Value;
                    config.ConnectionString = busConfig.ConnectionString;
                    config.PublishTo = busConfig.TopicName;
                    config.MessageOptions = (message, busMessage) => busMessage.SessionId = message.OrderId.ToString();
                }
            );

        services
            .RegisterServiceBusMessagePublisher<CreateOrderMessage>("q-orders")
            .Configure<IOptions<ServiceBusConfig>>(
                (config, busConfigOptions) =>
                {
                    var busConfig = busConfigOptions.Value;
                    config.ConnectionString = busConfig.ConnectionString;
                    config.PublishTo = busConfig.QueueName;
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
