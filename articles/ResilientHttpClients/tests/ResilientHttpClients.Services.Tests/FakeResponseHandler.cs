using System.Net;

namespace ResilientHttpClients.Services.Tests;

public class FakeResponseHandler(HttpStatusCode statusCode, Func<HttpContent> contentFunc)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // On retry, simulate a successful response.
        var httpResponse = new HttpResponseMessage(statusCode) { Content = contentFunc() };
        return Task.FromResult(httpResponse);
    }
}
