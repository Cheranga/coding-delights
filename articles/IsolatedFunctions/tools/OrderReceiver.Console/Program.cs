using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using OrderReceiver.Console;
using OrderReceiver.Console.Models;
using OrderReceiver.Console.Services;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(builder =>
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddUserSecrets<Program>()
    )
    .ConfigureServices((context, services) => services.RegisterDependencies(context.Configuration))
    .Build();

await host.RunAsync();
