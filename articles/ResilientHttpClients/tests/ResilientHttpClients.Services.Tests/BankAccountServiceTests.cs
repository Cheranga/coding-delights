using System.Net;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Moq;
using Polly;
using ResilientHttpClients.Services.Models;
using RichardSzalay.MockHttp;
using WireMock;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.ResponseProviders;
using WireMock.Server;
using WireMock.Types;
using WireMock.Util;

namespace ResilientHttpClients.Services.Tests;

public class BankAccountServiceTests
{
    [Fact]
    public async Task Test1()
    {
        var capturedRequests = new List<IRequestMessage>();
        var wireMockServer = WireMockServer.Start();

        wireMockServer
            .Given(Request.Create().WithPath("/api/token"))
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("new-token"));

        var calledTokens = new List<string>();
        var mockedCache = new Mock<IDistributedCache>();
        mockedCache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-token"u8.ToArray());
        mockedCache
            .Setup(x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .Callback<string, byte[], DistributedCacheEntryOptions, CancellationToken>(
                (_, value, _, _) =>
                {
                    calledTokens.Add(Encoding.UTF8.GetString(value));
                }
            );

        var services = new ServiceCollection();
        services.AddSingleton(mockedCache.Object);

        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<TokenHeaderMiddleware>();

        services.AddHttpClient(
            "tokenservice",
            client =>
            {
                client.BaseAddress = new Uri(wireMockServer.Urls[0]);
            }
        );

        wireMockServer
            .Given(Request.Create().WithPath("/api/accounts"))
            .InScenario(1)
            .WillSetStateTo(1)
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));

        wireMockServer
            .Given(Request.Create().WithPath("/api/accounts"))
            .InScenario(1)
            .WhenStateIs(1)
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(
                        new ListBankAccountsResponse
                        {
                            BankAccounts = new List<BankAccountResponse>
                            {
                                new()
                                {
                                    AccountName = "Bruce Wayne",
                                    AccountNumber = "1234567890",
                                    Balance = 5000000,
                                },
                            },
                        }
                    )
            );

        services
            .AddHttpClient<IBankAccountService, BankAccountService>(client =>
                client.BaseAddress = new Uri(wireMockServer.Urls[0])
            )
            .AddHttpMessageHandler<TokenHeaderMiddleware>()
            .AddResilienceHandler(
                "retryPipeline",
                (builder, context) =>
                {
                    var tokenService = context.ServiceProvider.GetRequiredService<ITokenService>();

                    builder.AddRetry(
                        new HttpRetryStrategyOptions
                        {
                            MaxRetryAttempts = 1,
                            BackoffType = DelayBackoffType.Linear,
                            Delay = TimeSpan.FromSeconds(1),
                            ShouldHandle = args =>
                                args.Outcome.Result is { StatusCode: HttpStatusCode.Unauthorized }
                                    ? PredicateResult.True()
                                    : PredicateResult.False(),
                            OnRetry = async _ =>
                            {
                                await tokenService.GetTokenAsync(CancellationToken.None, true);
                            },
                        }
                    );
                }
            );

        var response = await services
            .BuildServiceProvider()
            .GetRequiredService<IBankAccountService>()
            .ListBankAccountsAsync(CancellationToken.None);

        // Assert that "calledTokens" contains "old-token" and "new-token" in order
        mockedCache.Verify(
            x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once
        );
        mockedCache.Verify(
            x =>
                x.SetAsync(
                    It.IsAny<string>(),
                    It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Once
        );
        Assert.Equal(new[] { "new-token" }, calledTokens);
    }
}
