using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

public sealed class ListBankAccountsResponse
{
    public required IReadOnlyList<BankAccountResponse> BankAccounts { get; init; }
}
