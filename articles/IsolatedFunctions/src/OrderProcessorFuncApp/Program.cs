using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Features;
using OrderProcessorFuncApp.Middlewares;
using Serilog;

Log.Logger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateBootstrapLogger();

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        builder.UseMiddleware<EnrichmentMiddleware>();
        var services = builder.Services;
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.WriteIndented = false;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
    })
    .ConfigureAppConfiguration(builder =>
    {
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);
        builder.AddUserSecrets<Program>();
    })
    .ConfigureServices(services =>
    {
        services.AddSingleton(
            new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                WriteIndented = false,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            }
        );
        services.AddSingleton<IOrderProcessor, OrderProcessor>();
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .ConfigureLogging(
        (context, logging) =>
        {
            Log.Logger = new LoggerConfiguration().ReadFrom.Configuration(context.Configuration).CreateLogger();

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

await host.RunAsync();
