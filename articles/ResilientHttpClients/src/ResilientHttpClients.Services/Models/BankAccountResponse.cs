namespace ResilientHttpClients.Services.Models;

public sealed class BankAccountResponse
{
    public required string AccountNumber { get; set; }
    public required string AccountName { get; set; }
    public required decimal Balance { get; set; }
}
