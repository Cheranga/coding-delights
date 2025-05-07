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
using Serilog;

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
#pragma warning disable S125
        // services.AddApplicationInsightsTelemetryWorkerService();
#pragma warning restore S125
        // services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
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
    })
    .UseSerilog(
        (context, configuration) =>
        {
            configuration
                .MinimumLevel.Information()
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .WriteTo.ApplicationInsights(context.Configuration["APPLICATIONINSIGHTS_CONNECTION_STRING"], TelemetryConverter.Traces);
        }
    )
    .Build();

await host.RunAsync();
