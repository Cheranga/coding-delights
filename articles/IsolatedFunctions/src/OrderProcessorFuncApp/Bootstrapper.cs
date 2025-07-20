using System.Text.Json;
using System.Text.Json.Serialization;
using Azure.Storage.Queues;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Configs;
using OrderProcessorFuncApp.Features.CreateOrder;
using OrderProcessorFuncApp.Infrastructure.Http;
using OrderProcessorFuncApp.Infrastructure.StorageQueues;
using OrderProcessorFuncApp.Middlewares;
using Serilog;

namespace OrderProcessorFuncApp;

public static class Bootstrapper
{
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
                builder.UseMiddleware<DtoRequestValidationMiddleware<CreateOrderRequestDto>>();

                var services = builder.Services;
                services.Configure<JsonSerializerOptions>(options =>
                {
                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                    options.PropertyNameCaseInsensitive = true;
                    options.WriteIndented = false;
                    options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
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
                    services.AddSingleton(
                        new JsonSerializerOptions
                        {
                            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                            WriteIndented = false,
                            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                        }
                    );
                    services.AddSingleton(typeof(IApiRequestReader<,>), typeof(ApiRequestReader<,>));
                    services.AddSingleton<IOrderApiResponseGenerator, OrderApiResponseGenerator>();
                    services.AddSingleton<ICreateOrderHandler, CreateOrderHandler>();
                    services.AddValidatorsFromAssembly(typeof(Program).Assembly);

                    services.AddApplicationInsightsTelemetryWorkerService();
                    services.ConfigureFunctionsApplicationInsights();

                    services.AddKeyedSingleton<IStorageQueuePublisher>(
                        "process-order",
                        (provider, _) =>
                        {
                            var storageConfig = context.Configuration.GetSection(nameof(StorageConfig)).Get<StorageConfig>();
                            ArgumentException.ThrowIfNullOrWhiteSpace(storageConfig?.ConnectionString);

                            var serializerOptions = provider.GetRequiredService<JsonSerializerOptions>();
                            var qServiceClient = new QueueServiceClient(
                                storageConfig.ConnectionString,
                                new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
                            );
                            var qClient = qServiceClient.GetQueueClient(storageConfig.ProcessingQueueName);

                            return new StorageQueuePublisher(
                                qClient,
                                serializerOptions,
                                provider.GetRequiredService<ILoggerFactory>().CreateLogger<StorageQueuePublisher>()
                            );
                        }
                    );

                    customRegistrations?.Invoke(context, services);
                }
            )
            .ConfigureLogging(
                (context, logging) =>
                {
                    var serilogLogger = new LoggerConfiguration().ReadFrom.Configuration(context.Configuration).CreateLogger();
                    Log.Logger = serilogLogger;

                    logging.AddSerilog(Log.Logger, true);

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
