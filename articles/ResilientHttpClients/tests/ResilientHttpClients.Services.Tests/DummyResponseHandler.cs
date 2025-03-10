using System.Collections.Immutable;
using System.Net;

namespace ResilientHttpClients.Services.Tests;

public class DummyResponseHandler(Queue<Func<HttpResponseMessage>> responseFuncs)
    : HttpMessageHandler
{
    private readonly List<HttpRequestMessage> _capturedRequests = new();

    public IReadOnlyList<HttpRequestMessage> CapturedRequests => _capturedRequests;

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (responseFuncs.TryDequeue(out var responseFunc))
        {
            _capturedRequests.Add(request);
            return Task.FromResult(responseFunc());
        }

        throw new Exception("No response handlers");
    }
}
