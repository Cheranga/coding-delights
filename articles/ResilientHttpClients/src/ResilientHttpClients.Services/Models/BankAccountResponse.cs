namespace ResilientHttpClients.Services.Models;

public sealed class BankAccountResponse
{
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
    public required decimal Balance { get; set; }
}

public sealed class TokenResponse
{
    public required string Token { get; set; }
}

public sealed class TokenSettings
{
    public required int TokenExpirationMinutes { get; set; } = 30;
}
