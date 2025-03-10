using System.Net;
using Microsoft.Extensions.DependencyInjection;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ResilientHttpClients.Services.Tests;

public class TokenHeaderMiddlewareTests : IDisposable
{
    private readonly WireMockServer _wireMockServer = WireMockServer.Start();
    private const string Token = nameof(Token);

    [Fact(DisplayName = "Token is added to request headers")]
    public async Task Test1() =>
        await Arrange(() =>
            {
                _wireMockServer
                    .Given(Request.Create().WithPath("/api/token"))
                    .RespondWith(
                        Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody(Token)
                    );

                _wireMockServer
                    .Given(Request.Create().UsingGet().WithPath("/api/orders"))
                    .RespondWith(
                        Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("orders")
                    );

                return _wireMockServer.Urls[0];
            })
            .And(data =>
            {
                var services = new ServiceCollection();
                services.AddDistributedMemoryCache();
                services.AddSingleton<TokenHeaderMiddleware>();
                services.AddSingleton<ITokenService, TokenService>();

                services.AddHttpClient(
                    "tokenservice",
                    client => client.BaseAddress = new Uri(data)
                );

                services
                    .AddHttpClient(
                        "orders",
                        client =>
                        {
                            client.BaseAddress = new Uri(data);
                        }
                    )
                    .AddHttpMessageHandler<TokenHeaderMiddleware>();

                var httpClient = services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("orders");

                var httpRequest = new HttpRequestMessage(HttpMethod.Get, "/api/orders");

                return (httpClient, httpRequest);
            })
            .Act(async data => await data.httpClient.SendAsync(data.httpRequest))
            .Assert(result => result.IsSuccessStatusCode)
            .And(async result =>
            {
                var responseContent = await result.Content.ReadAsStringAsync();
                Assert.Equal("orders", responseContent);
            })
            .And(
                (data, _) =>
                {
                    Assert.True(
                        data.httpRequest.Headers.TryGetValues("Authorization", out var headerValues)
                    );
                    Assert.Equal($"Bearer {Token}", headerValues.Single());
                }
            );

    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }
}
