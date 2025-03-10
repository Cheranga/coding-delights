using System.Net;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Moq;
using ResilientHttpClients.Services.Models;
using RichardSzalay.MockHttp;
using Xunit.Abstractions;

namespace ResilientHttpClients.Services.Tests;

public class TokenServiceTests(ITestOutputHelper logger)
{
    private const string TokenBaseAddress = "https://blah.com";
    private const string oldToken = nameof(oldToken);
    private const string newToken = nameof(newToken);

    [Fact(
        DisplayName = "Token is available in the cache, forceRefresh is false, and the cached token must be returned"
    )]
    public async Task Test1() =>
        await Arrange(() =>
            {
                var mockedHttp = new MockHttpMessageHandler();
                mockedHttp
                    .When($"{TokenBaseAddress}/api/token")
                    .Respond(HttpStatusCode.OK, new StringContent(newToken));

                return mockedHttp;
            })
            .And(data =>
            {
                var mockedCache = new Mock<IDistributedCache>();
                mockedCache
                    .Setup(x => x.GetAsync("ApiToken", It.IsAny<CancellationToken>()))
                    .ReturnsAsync(Encoding.UTF8.GetBytes(oldToken));

                return (mockedHttp: data, mockedCache);
            })
            .And(data =>
            {
                var tokenSettings = Mock.Of<IOptionsMonitor<TokenSettings>>(x =>
                    x.CurrentValue == new TokenSettings { TokenExpirationMinutes = 30 }
                );

                return (data.mockedCache, data.mockedHttp, tokenSettings);
            })
            .And(data =>
            {
                var client = data.mockedHttp.ToHttpClient();
                client.BaseAddress = new Uri(TokenBaseAddress);
                var tokenService = new TokenService(
                    data.tokenSettings,
                    Mock.Of<IHttpClientFactory>(factory =>
                        factory.CreateClient("tokenservice") == client
                    ),
                    data.mockedCache.Object
                );
                return (data.mockedHttp, data.mockedCache, tokenService);
            })
            .Act(async data => await data.tokenService.GetTokenAsync(CancellationToken.None))
            .Assert(result => result.Token == oldToken)
            .And(
                (data, _) =>
                    data.mockedCache.Verify(
                        x => x.GetAsync("ApiToken", It.IsAny<CancellationToken>()),
                        Times.Once
                    )
            );

    [Fact(
        DisplayName = "Token is available in the cache, forceRefresh is true, and a new token must be returned"
    )]
    public async Task Test2() =>
        await Arrange(() =>
            {
                var mockedHttp = new MockHttpMessageHandler();
                mockedHttp
                    .When($"{TokenBaseAddress}/api/token")
                    .Respond(HttpStatusCode.OK, new StringContent(newToken));
                var httpClient = mockedHttp.ToHttpClient();
                httpClient.BaseAddress = new Uri(TokenBaseAddress);

                return httpClient;
            })
            .And(data =>
            {
                var mockedCache = new Mock<IDistributedCache>();

                return (mockedHttpClient: data, mockedCache);
            })
            .And(data =>
            {
                var tokenSettings = Mock.Of<IOptionsMonitor<TokenSettings>>(x =>
                    x.CurrentValue == new TokenSettings { TokenExpirationMinutes = 30 }
                );

                return (data.mockedCache, data.mockedHttpClient, tokenSettings);
            })
            .And(data =>
            {
                var tokenService = new TokenService(
                    data.tokenSettings,
                    Mock.Of<IHttpClientFactory>(factory =>
                        factory.CreateClient("tokenservice") == data.mockedHttpClient
                    ),
                    data.mockedCache.Object
                );
                return (tokenService, data.mockedCache);
            })
            .Act(async data => await data.tokenService.GetTokenAsync(CancellationToken.None, true))
            .Assert(result => result.Token == newToken)
            .And(
                (data, _) =>
                    data.mockedCache.Verify(
                        x => x.GetAsync("ApiToken", It.IsAny<CancellationToken>()),
                        Times.Never
                    )
            )
            .And(
                (data, _) =>
                    data.mockedCache.Verify(
                        x =>
                            x.SetAsync(
                                "ApiToken",
                                Encoding.UTF8.GetBytes(newToken),
                                It.IsAny<DistributedCacheEntryOptions>(),
                                It.IsAny<CancellationToken>()
                            ),
                        Times.Once
                    )
            );
}
