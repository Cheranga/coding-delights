using Microsoft.Extensions.Hosting;
using OrderProcessorFuncApp;
using Serilog;

var bootstrapLogger = new LoggerConfiguration().Enrich.FromLogContext().WriteTo.Console().CreateBootstrapLogger();
Log.Logger = bootstrapLogger;

using var host = Bootstrapper.GetHost();
await host.RunAsync();
await Log.CloseAndFlushAsync();
