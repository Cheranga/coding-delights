using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Polly.Registry;
using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

public interface IBankAccountService
{
    Task<ListBankAccountsResponse> ListBankAccountsAsync(CancellationToken token);
}

internal sealed class BankAccountService(
    HttpClient client,
    ResiliencePipelineProvider<string> pipelineProvider,
    ILogger<BankAccountService> logger
) : IBankAccountService
{
    public async Task<ListBankAccountsResponse> ListBankAccountsAsync(CancellationToken token)
    {
        var policy = pipelineProvider.GetPipeline<HttpResponseMessage>("pipeline");

        var httpResponse = await policy.ExecuteAsync(
            async ct => await client.GetAsync("/api/accounts", ct),
            token
        );

        var bankAccounts = await httpResponse.Content.ReadFromJsonAsync<ListBankAccountsResponse>(
            token
        );
        return bankAccounts ?? ListBankAccountsResponse.Empty;
    }
}
