using System.Net.Http.Headers;

namespace TestProject1;

public class TokenHeaderMiddleware : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // Add token header if it's not already present
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Guid.NewGuid().ToString("N")
        );
        return await base.SendAsync(request, cancellationToken);
    }
}
