using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

public interface IBankAccountService
{
    Task<ListBankAccountsResponse> ListBankAccountsAsync(CancellationToken token);
}
