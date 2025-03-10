using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Moq;
using Polly;
using ResilientHttpClients.Services.Models;
using RichardSzalay.MockHttp;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ResilientHttpClients.Services.Tests;

public class BankAccountServiceTests
{
    private const string BaseUrl = "https://gothamnationalbank.com";

    [Fact]
    public async Task Test1()
    {
        var wireMockServer = WireMockServer.Start();
        wireMockServer
            .Given(Request.Create().WithPath("/api/token"))
            .InScenario(0)
            .WillSetStateTo(1)
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("old-token"));

        wireMockServer
            .Given(Request.Create().WithPath("/api/token"))
            .InScenario(0)
            .WhenStateIs(1)
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("new-token"));

        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();

        var mockedTokenService = new Mock<ITokenService>();
        mockedTokenService
            .SetupSequence(x => x.GetTokenAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>()))
            .ReturnsAsync(new TokenResponse { Token = "old-token" })
            .ReturnsAsync(new TokenResponse { Token = "new-token" });
        services.AddSingleton(mockedTokenService.Object);
        services.AddSingleton<TokenHeaderMiddleware>();

        // var mockedTokenHttp = new MockHttpMessageHandler();
        // mockedTokenHttp
        //     .When(HttpMethod.Get, $"{BaseUrl}/api/token")
        //     .Respond(HttpStatusCode.OK, new StringContent("old-token"));
        // mockedTokenHttp
        //     .When(HttpMethod.Get, $"{BaseUrl}/api/token")
        //     .Respond(HttpStatusCode.OK, new StringContent("new-token"));
        services.AddHttpClient(
            "tokenservice",
            client =>
            {
                client.BaseAddress = new Uri(wireMockServer.Urls[0]);
            }
        );

        // var mockedBankAccountHttp = new MockHttpMessageHandler();
        // mockedBankAccountHttp
        //     .When(HttpMethod.Get, $"{BaseUrl}/api/accounts")
        //     .Respond(HttpStatusCode.Unauthorized);
        // mockedBankAccountHttp
        //     .When(HttpMethod.Get, $"{BaseUrl}/api/accounts")
        //     .Respond(HttpStatusCode.OK, JsonContent.Create(new ListBankAccountsResponse
        //     {
        //         BankAccounts = new List<BankAccountResponse>
        //         {
        //             new()
        //             {
        //                 AccountName = "Bruce Wayne",
        //                 AccountNumber = "1234567890",
        //                 Balance = 5000000
        //             }
        //         }
        //     }));

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
            // .AddHttpClient("bankaccounts", client => { client.BaseAddress = new Uri(wireMockServer.Urls[0]); })
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

        // var httpClient = services.BuildServiceProvider().GetRequiredService<IHttpClientFactory>().CreateClient("bankaccounts");
        // var request = new HttpRequestMessage(HttpMethod.Get, $"{BaseUrl}/api/accounts");
        // var httpResponse = await httpClient.SendAsync(request);

        var response = await services
            .BuildServiceProvider()
            .GetRequiredService<IBankAccountService>()
            .ListBankAccountsAsync(CancellationToken.None);
        mockedTokenService.Verify(
            x => x.GetTokenAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>()),
            Times.Exactly(2)
        );
    }
}
