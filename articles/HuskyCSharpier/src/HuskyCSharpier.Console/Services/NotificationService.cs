using Microsoft.Extensions.Logging;

namespace HuskyCSharpier.Console.Services;

public class NotificationService : INotificationService
{
    private readonly ILogger<NotificationService> _logger;
    public NotificationService(ILogger<NotificationService> logger) => _logger = logger;
    public async Task SendNotificationAsync(NotificationOptions options)
    {
        _logger.LogInformation("Sending notification from {Sender} to {Recipient} with message: {Message}", options.Sender, options.Recipient, options.Message);
        await Task.CompletedTask;
    }
}