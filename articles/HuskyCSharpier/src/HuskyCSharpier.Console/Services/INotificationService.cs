namespace HuskyCSharpier.Console.Services;

public record NotificationOptions(string Sender, string Recipient, string Message);

public interface INotificationService
{
    Task SendNotificationAsync(NotificationOptions options);
}
