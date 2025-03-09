using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Moq;
using ResilientHttpClients.Services.Models;
using RichardSzalay.MockHttp;

namespace ResilientHttpClients.Services.Tests;

public class TokenHeaderMiddlewareTests
{
    private const string BaseAddress = "https://blah.com";
    private const string Token = nameof(Token);

    [Fact]
    public async Task Test1() =>
        await Arrange(() =>
            {
                var mockedHttp = new MockHttpMessageHandler();
                mockedHttp
                    .When(HttpMethod.Get, $"{BaseAddress}/api/token")
                    .Respond(HttpStatusCode.OK, new StringContent(Token));

                var services = new ServiceCollection();
                services.AddSingleton(
                    Mock.Of<IDistributedCache>(cache =>
                        cache.GetAsync("ApiToken", It.IsAny<CancellationToken>())
                        == Task.FromResult(Encoding.UTF8.GetBytes(Token))
                    )
                );
                services.AddSingleton(Mock.Of<IOptionsMonitor<TokenSettings>>());
                services
                    .AddHttpClient<ITokenService, TokenService>()
                    .ConfigurePrimaryHttpMessageHandler(() => mockedHttp);

                services.AddSingleton<TokenHeaderMiddleware>();
                services.AddSingleton(
                    new DummyResponseHandler(HttpStatusCode.OK, () => new StringContent("Orders"))
                );

                services
                    .AddHttpClient(
                        "orders",
                        client =>
                        {
                            client.BaseAddress = new Uri(BaseAddress);
                        }
                    )
                    .AddHttpMessageHandler<TokenHeaderMiddleware>()
                    .ConfigurePrimaryHttpMessageHandler<DummyResponseHandler>();

                var httpClient = services
                    .BuildServiceProvider()
                    .GetRequiredService<IHttpClientFactory>()
                    .CreateClient("orders");

                var httpRequest = new HttpRequestMessage(
                    HttpMethod.Get,
                    $"{BaseAddress}/api/orders"
                );

                return (httpClient, httpRequest);
            })
            .Act(async data => await data.httpClient.SendAsync(data.httpRequest))
            .Assert(result => result.IsSuccessStatusCode)
            .And(async result =>
                string.Equals(
                    "Orders",
                    await result.Content.ReadAsStringAsync(),
                    StringComparison.Ordinal
                )
            )
            .And(
                (data, _) =>
                {
                    Assert.True(
                        data.httpRequest.Headers.TryGetValues("Authorization", out var headerValues)
                    );
                    Assert.Equal($"Bearer {Token}", headerValues.Single());
                }
            );
}
