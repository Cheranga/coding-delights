using System.Net.Http.Json;
using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

internal sealed class OtherBankAccountService(HttpClient client) : IBankAccountService
{
    public async Task<ListBankAccountsResponse> ListBankAccountsAsync(CancellationToken token)
    {
        var httpResponse = await client.GetAsync("/api/accounts", token);
        var bankAccounts = await httpResponse.Content.ReadFromJsonAsync<ListBankAccountsResponse>(token);
        return bankAccounts ?? ListBankAccountsResponse.Empty;
    }
}
