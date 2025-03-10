using System.Net;

namespace ResilientHttpClients.Services.Tests;

public class DummyResponseHandler(HttpStatusCode statusCode, Func<HttpContent> contentFunc)
    : HttpMessageHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        var httpResponse = new HttpResponseMessage(statusCode) { Content = contentFunc() };
        return Task.FromResult(httpResponse);
    }
}
