using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Messaging.ServiceBus;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

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
        services.TryAddSingleton<IMessagePublisher, MessagePublisher>();
        return services;
    }

    private static IServiceCollection RegisterConfigurations(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<ServiceBusConfig>().BindConfiguration(nameof(ServiceBusConfig));
        return services;
    }

    private static IServiceCollection RegisterInfrastructureServices(this IServiceCollection services) =>
        services.AddSingleton(sp =>
        {
            var config = sp.GetRequiredService<IOptions<ServiceBusConfig>>().Value;
            return new ServiceBusClient(config.ConnectionString);
        });

    public static void RegisterDependencies(this IServiceCollection services, IConfiguration configuration)
    {
        services.RegisterConfigurations(configuration).RegisterInfrastructureServices().RegisterApplicationServices();
    }
}
