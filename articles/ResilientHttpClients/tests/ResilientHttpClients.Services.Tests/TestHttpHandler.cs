namespace ResilientHttpClients.Services.Tests;

internal sealed class TestHttpHandler : HttpMessageHandler
{
    private readonly Queue<Func<HttpResponseMessage>> _responses;

    public readonly List<HttpRequestMessage> _capturedRequests = new List<HttpRequestMessage>();

    public TestHttpHandler(Queue<Func<HttpResponseMessage>> responses)
    {
        _responses = responses;
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        if (_responses.TryDequeue(out var func))
        {
            _capturedRequests.Add(request);
            return Task.FromResult(func());
        }

        throw new Exception("No response queued");
    }

    public IReadOnlyList<HttpRequestMessage> CapturedRequests => _capturedRequests;
}
