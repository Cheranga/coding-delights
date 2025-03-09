using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

public interface IBankAccountService
{
    Task<ListBankAccountsResponse> ListBankAccountsAsync(CancellationToken token);
}

internal sealed class BankAccountService(HttpClient client, ILogger<BankAccountService> logger)
    : IBankAccountService
{
    public async Task<ListBankAccountsResponse> ListBankAccountsAsync(CancellationToken token)
    {
        var response = await client.GetAsync("/api/accounts", token);
        var bankAccounts = await response.Content.ReadFromJsonAsync<ListBankAccountsResponse>(
            token
        );
        logger.LogInformation("Retrieved bank accounts: {@BankAccounts}", bankAccounts);
        return bankAccounts
            ?? new ListBankAccountsResponse { BankAccounts = new List<BankAccountResponse>() };
    }
}
