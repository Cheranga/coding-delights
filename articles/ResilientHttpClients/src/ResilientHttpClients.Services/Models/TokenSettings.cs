namespace ResilientHttpClients.Services.Models;

public sealed class TokenSettings
{
    public required int TokenExpirationMinutes { get; set; } = 30;
}
