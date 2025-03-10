namespace ResilientHttpClients.Services.Models;

public sealed class BankAccountResponse
{
    public required string AccountNumber { get; init; }
    public required string AccountName { get; init; }
    public required decimal Balance { get; init; }
}

public sealed class TokenResponse
{
    public required string Token { get; init; }
}

public sealed class TokenSettings
{
    public required int TokenExpirationMinutes { get; set; } = 30;
}
