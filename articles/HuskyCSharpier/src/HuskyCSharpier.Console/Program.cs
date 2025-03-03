using HuskyCSharpier.Console.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

var host = Host.CreateDefaultBuilder()
    .ConfigureServices(services => { services.AddSingleton<INotificationService, NotificationService>(); }).Build();

var notificationService = host.Services.GetRequiredService<INotificationService>();

await SendNotificationAsync(notificationService, new NotificationOptions("James Gordon", "Batman", "Bat Signal!"));

await host.RunAsync();

static Task SendNotificationAsync(INotificationService notificationService,
    NotificationOptions options) => notificationService.SendNotificationAsync(options);