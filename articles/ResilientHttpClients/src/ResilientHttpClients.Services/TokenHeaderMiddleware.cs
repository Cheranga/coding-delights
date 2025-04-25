using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace ResilientHttpClients.Services;

public class TokenHeaderMiddleware(ITokenService tokenService, ILogger<TokenHeaderMiddleware> logger) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        logger.LogInformation("Adding token to request");
        var token = await tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token.Token);
        logger.LogInformation("Token added to request");
        return await base.SendAsync(request, cancellationToken);
    }
}
