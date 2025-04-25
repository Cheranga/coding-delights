namespace ResilientHttpClients.Services.Models;

public sealed class ListBankAccountsResponse
{
    public required IReadOnlyList<BankAccountResponse> BankAccounts { get; set; }

    public static ListBankAccountsResponse Empty => new ListBankAccountsResponse { BankAccounts = [] };
}
