using WireMock;
using WireMock.ResponseBuilders;
using WireMock.ResponseProviders;
using WireMock.Settings;

namespace ResilientHttpClients.Services.Tests;

internal sealed class CustomResponseProvider : IResponseProvider
{
    private readonly Queue<Func<IResponseBuilder>> _responses = new();
    private readonly List<IRequestMessage> _capturedRequests = new();

    private CustomResponseProvider(Queue<Func<IResponseBuilder>> responses)
    {
        _responses = responses;
    }

    public static CustomResponseProvider New(params Func<IResponseBuilder>[] funcs)
    {
        return new CustomResponseProvider(new Queue<Func<IResponseBuilder>>(funcs));
    }

    public IReadOnlyList<IRequestMessage> CapturedRequests => _capturedRequests;

    public async Task<(IResponseMessage Message, IMapping? Mapping)> ProvideResponseAsync(
        IMapping mapping,
        IRequestMessage requestMessage,
        WireMockServerSettings settings
    )
    {
        if (_responses.TryDequeue(out var responseFunc))
        {
            _capturedRequests.Add(requestMessage);
            return await responseFunc().ProvideResponseAsync(mapping, requestMessage, settings);
        }

        throw new Exception("No response queued");
    }
}
