using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Infrastructure.Http;
using OrderProcessorFuncApp.Infrastructure.StorageQueues;
using OrderProcessorFuncApp.Middlewares;
using Serilog;

namespace OrderProcessorFuncApp;

public static class Bootstrapper
{
    private static readonly JsonSerializerOptions DefaultJsonSerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        PropertyNameCaseInsensitive = true,
        TypeInfoResolverChain = { AppJsonContext.Default, new DefaultJsonTypeInfoResolver() },
    };

    public static IHost GetHost(
        Action<HostBuilderContext, IServiceCollection>? customRegistrations = null,
        Action<HostBuilderContext, IConfigurationBuilder>? customConfigurations = null
    )
    {
        var host = new HostBuilder()
            .ConfigureFunctionsWorkerDefaults(builder =>
            {
                builder.UseMiddleware<PerformanceMonitoringMiddleware>();
                builder.UseMiddleware<EnrichmentMiddleware>();
                var services = builder.Services;
                services.Configure<JsonSerializerOptions>(options =>
                {
                    options.PropertyNamingPolicy = DefaultJsonSerializerOptions.PropertyNamingPolicy;
                    options.PropertyNameCaseInsensitive = DefaultJsonSerializerOptions.PropertyNameCaseInsensitive;
                    options.WriteIndented = DefaultJsonSerializerOptions.WriteIndented;
                    options.DefaultIgnoreCondition = DefaultJsonSerializerOptions.DefaultIgnoreCondition;
                });
            })
            .ConfigureAppConfiguration(
                (context, builder) =>
                {
                    builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
                    builder.AddUserSecrets<Program>();

                    customConfigurations?.Invoke(context, builder);
                }
            )
            .ConfigureServices(
                (context, services) =>
                {
                    services.AddSingleton(DefaultJsonSerializerOptions);
                    services.AddSingleton(typeof(IApiRequestReader<,>), typeof(ApiRequestReader<,>));
                    services.AddSingleton(typeof(IStorageQueueReader<>), typeof(StorageQueueReader<>));
                    services.AddSingleton<IOrderApiResponseGenerator, OrderApiResponseGenerator>();
                    services.AddValidatorsFromAssembly(typeof(Program).Assembly);

                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();

                    customRegistrations?.Invoke(context, services);
                }
            )
            .ConfigureLogging(
                (context, logging) =>
                {
                    var serilogLogger = new LoggerConfiguration().ReadFrom.Configuration(context.Configuration).CreateLogger();
                    Log.Logger = serilogLogger;

                    logging.ClearProviders().AddSerilog(Log.Logger, true);

                    logging.Services.Configure<LoggerFilterOptions>(options =>
                    {
                        var defaultRule = options.Rules.FirstOrDefault(rule =>
                            string.Equals(
                                rule.ProviderName,
                                "Microsoft.Extensions.Logging.ApplicationInsights.ApplicationInsightsLoggerProvider",
                                StringComparison.OrdinalIgnoreCase
                            )
                        );
                        if (defaultRule is not null)
                        {
                            options.Rules.Remove(defaultRule);
                        }
                    });
                }
            )
            .Build();

        return host;
    }
}
