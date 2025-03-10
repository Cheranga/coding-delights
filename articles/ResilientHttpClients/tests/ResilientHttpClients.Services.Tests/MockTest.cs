using System.Collections.Immutable;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http.Json;
using ResilientHttpClients.Services.Models;
using RichardSzalay.MockHttp;

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

public sealed class MockTest
{
    [Fact]
    public async Task Test2()
    {
        var baseUrl = "https://localhost:3000";
        var testHttpHandler = new TestHttpHandler(
            new Queue<Func<HttpResponseMessage>>(
                [
                    () =>
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(new TokenResponse { Token = "old-token" }),
                        },
                    () =>
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(new TokenResponse { Token = "new-token" }),
                        },
                ]
            )
        );
        var httpClient = new HttpClient(testHttpHandler) { BaseAddress = new Uri(baseUrl) };

        var response1 = await httpClient.GetFromJsonAsync<TokenResponse>("/api/token");
        var response2 = await httpClient.GetFromJsonAsync<TokenResponse>("/api/token");
    }

    [Fact]
    public async Task Test1()
    {
        var baseUrl = "https://localhost:3000";
        var capturedRequests = new List<HttpRequestMessage>();
        var mockHttp = new MockHttpMessageHandler();
        mockHttp
            .When(HttpMethod.Get, $"{baseUrl}/api/token")
            .Respond(
                HttpStatusCode.OK,
                req =>
                {
                    capturedRequests.Add(req);
                    return JsonContent.Create(new TokenResponse { Token = "old-token" });
                }
            );

        mockHttp
            .When(HttpMethod.Get, $"{baseUrl}/api/token")
            .Respond(
                HttpStatusCode.OK,
                req =>
                {
                    capturedRequests.Add(req);
                    return JsonContent.Create(new TokenResponse { Token = "new-token" });
                }
            );

        var client = mockHttp.ToHttpClient();
        var response1 = await client.GetFromJsonAsync<TokenResponse>($"{baseUrl}/api/token");
        var response2 = await client.GetFromJsonAsync<TokenResponse>($"{baseUrl}/api/token");

        Assert.Equal("old-token", response1?.Token);
        Assert.Equal("new-token", response2?.Token);
    }
}
