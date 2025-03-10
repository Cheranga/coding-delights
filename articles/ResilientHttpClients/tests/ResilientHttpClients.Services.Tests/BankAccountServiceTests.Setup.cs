using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Moq;
using Polly;
using ResilientHttpClients.Services.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ResilientHttpClients.Services.Tests;

public partial class BankAccountServiceTests : IDisposable
{
    private readonly WireMockServer _wireMockServer = WireMockServer.Start();
    private readonly Mock<IDistributedCache> _mockedCache = new();

    private static void SetupTokenRequestResponses(WireMockServer server)
    {
        // server
        //     .Given(Request.Create().WithPath("/api/token"))
        //     .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("new-token"));
    }

    private static void SetupOrdersRequestResponses(WireMockServer server)
    {
        // server
        //     .Given(Request.Create().WithPath("/api/accounts"))
        //     .InScenario(1)
        //     .WillSetStateTo(1)
        //     .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));
        //
        // server
        //     .Given(Request.Create().WithPath("/api/accounts"))
        //     .InScenario(1)
        //     .WhenStateIs(1)
        //     .RespondWith(
        //         Response
        //             .Create()
        //             .WithStatusCode(HttpStatusCode.OK)
        //             .WithBodyAsJson(
        //                 new ListBankAccountsResponse
        //                 {
        //                     BankAccounts = new List<BankAccountResponse>
        //                     {
        //                         new()
        //                         {
        //                             AccountName = "Bruce Wayne",
        //                             AccountNumber = "1234567890",
        //                             Balance = 5000000,
        //                         },
        //                     },
        //                 }
        //             )
        //     );
    }

    private static void SetupMockedCache(Mock<IDistributedCache> cache)
    {
        cache
            .Setup(x => x.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("old-token"u8.ToArray());
    }

    private IBankAccountService GetBankAccountService()
    {
        var baseUrl = _wireMockServer.Urls[0];
        var services = new ServiceCollection();
        services.AddSingleton(_mockedCache.Object);
        services.AddSingleton<ITokenService, TokenService>();
        services.AddSingleton<TokenHeaderMiddleware>();

        var dummyTokensResponseHandler = new DummyResponseHandler(
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

        services
            .AddHttpClient(
                "tokenservice",
                client =>
                {
                    client.BaseAddress = new Uri(baseUrl);
                }
            )
            .ConfigurePrimaryHttpMessageHandler(() => dummyTokensResponseHandler);

        var dummyAccountsResponseHandler = new DummyResponseHandler(
            new Queue<Func<HttpResponseMessage>>(
                [
                    () => new HttpResponseMessage(HttpStatusCode.Unauthorized),
                    () =>
                        new HttpResponseMessage(HttpStatusCode.OK)
                        {
                            Content = JsonContent.Create(
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
                            ),
                        },
                ]
            )
        );
        services
            .AddHttpClient<IBankAccountService, BankAccountService>()
            .ConfigureHttpClient(client => client.BaseAddress = new Uri(baseUrl))
            .ConfigurePrimaryHttpMessageHandler(() => dummyAccountsResponseHandler)
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

        return services.BuildServiceProvider().GetRequiredService<IBankAccountService>();
    }

    private void ConfigureHandler(HttpMessageHandler arg1, IServiceProvider arg2)
    {
        throw new NotImplementedException();
    }

    public void Dispose()
    {
        _wireMockServer.Stop();
        _wireMockServer.Dispose();
    }
}
