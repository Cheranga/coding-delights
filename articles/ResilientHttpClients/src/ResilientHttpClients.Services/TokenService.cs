using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services;

public interface ITokenService
{
    Task<TokenResponse> GetTokenAsync(CancellationToken token, bool forceRefresh = false);
}

internal sealed class TokenService(
    IOptionsMonitor<TokenSettings> options,
    IHttpClientFactory clientFactory,
    IDistributedCache cache
) : ITokenService
{
    private readonly TokenSettings _tokenSettings = options.CurrentValue;
    private readonly HttpClient _client = clientFactory.CreateClient("tokenservice");

    public async Task<TokenResponse> GetTokenAsync(
        CancellationToken token,
        bool forceRefresh = false
    )
    {
        if (
            !forceRefresh
            && await cache.GetStringAsync("ApiToken", token) is { Length: > 0 } cachedToken
        )
        {
            return new TokenResponse { Token = cachedToken };
        }

        var apiToken = await _client.GetStringAsync("/api/token", token);
        await cache.SetStringAsync(
            "ApiToken",
            apiToken,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(
                    _tokenSettings.TokenExpirationMinutes
                ),
            },
            token
        );

        return new TokenResponse { Token = apiToken };
    }
}
