using System.Text.Json;
using System.Text.Json.Serialization;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Features;
using Serilog;
using Serilog.Configuration;
using Serilog.Events;

Log.Logger = new LoggerConfiguration().WriteTo.Console().CreateBootstrapLogger();

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
        
#pragma warning disable S125
        // services.AddSingleton<ITelemetryInitializer, CustomTelemetryInitializer>();
#pragma warning restore S125
    })
    .ConfigureLogging((context,logging) =>
    {
        Log.Logger = new LoggerConfiguration()
            .ReadFrom.Configuration(context.Configuration)
            .CreateLogger();
        
        // Log.Logger = new LoggerConfiguration()
        //     .MinimumLevel.Information()
        //     .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
        //     .MinimumLevel.Override("Worker", LogEventLevel.Warning)
        //     .MinimumLevel.Override("Host", LogEventLevel.Warning)
        //     .MinimumLevel.Override("System", LogEventLevel.Warning)
        //     .MinimumLevel.Override("Function", LogEventLevel.Warning)
        //     .MinimumLevel.Override("Azure*", LogEventLevel.Warning)
        //     .MinimumLevel.Override("OrderProcessorFuncApp.Features", LogEventLevel.Information)
        //     .Enrich.FromLogContext()
        //     .Enrich.WithProperty("FunctionAppName", "OrderProcessorFuncApp")
        //     .WriteTo.Console()
        //     .WriteTo.ApplicationInsights(TelemetryConverter.Traces, LogEventLevel.Information)
#pragma warning disable S125
        //     .CreateLogger();
#pragma warning restore S125
#pragma warning disable S4663
        //
#pragma warning restore S4663
        logging.AddSerilog(Log.Logger, true);

        // Remove the default Application Insights logger provider so that Information logs are sent
        // https://learn.microsoft.com/en-us/azure/azure-functions/dotnet-isolated-process-guide?tabs=hostbuilder%2Clinux&WT.mc_id=DOP-MVP-5001655#managing-log-levels
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
    })
    .Build();

await host.RunAsync();
