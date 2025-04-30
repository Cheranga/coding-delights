using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

public interface ITokenService
{
    Task<TokenResponse> GetTokenAsync(CancellationToken token, bool forceRefresh = false);
}
