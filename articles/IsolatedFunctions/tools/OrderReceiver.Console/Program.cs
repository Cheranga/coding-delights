using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using OrderReceiver.Console;

var host = Host.CreateDefaultBuilder()
    .ConfigureAppConfiguration(builder =>
        builder.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true).AddUserSecrets<Program>()
    )
    .ConfigureServices((context, services) => services.RegisterDependencies(context.Configuration))
    .Build();

await host.RunAsync();
