using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.ApplicationInsights;
using OrderProcessorFuncApp.Core;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults(builder =>
    {
        var services = builder.Services;
        services.Configure<JsonSerializerOptions>(options =>
        {
            options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PropertyNameCaseInsensitive = true;
            options.WriteIndented = false;
            options.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
        });
        services.AddApplicationInsightsTelemetryWorkerService();
        services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
    })
    .ConfigureAppConfiguration(builder =>
    {
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
        services.AddValidatorsFromAssembly(typeof(Program).Assembly);
        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .ConfigureLogging(builder =>
    {
        builder.ClearProviders();
        builder.AddJsonConsole();
        builder.AddApplicationInsights();
    })
    .Build();

await host.RunAsync();
