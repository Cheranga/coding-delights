using System.Collections;
using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Bogus;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using Polly.Registry;
using ResilientHttpClients.Services.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ResilientHttpClients.Services.Tests;

public class NewBankAccountServiceTests
{
    [Fact]
    public async Task Test1()
    {
        var wiremockServer = WireMockServer.Start();
        var baseUrl = wiremockServer.Urls[0];

        var tokenResponseProvider = CustomResponseProvider.New(
            () => Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("new-token")
        );

        var expectedBankAccountsResponse = new ListBankAccountsResponse
        {
            BankAccounts = new AutoFaker<BankAccountResponse>().Generate(3),
        };
        var bankAccountResponseProvider = CustomResponseProvider.New(
            () => Response.Create().WithStatusCode(HttpStatusCode.Unauthorized),
            () =>
                Response
                    .Create()
                    .WithStatusCode(HttpStatusCode.OK)
                    .WithBodyAsJson(expectedBankAccountsResponse)
        );

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/token"))
            .RespondWith(tokenResponseProvider);

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/accounts"))
            .InScenario(1)
            .WillSetStateTo(1)
            .RespondWith(bankAccountResponseProvider);

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/accounts"))
            .InScenario(1)
            .WhenStateIs(1)
            .RespondWith(bankAccountResponseProvider);

        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton(
            Mock.Of<IOptionsMonitor<TokenSettings>>(builder =>
                builder.CurrentValue == new TokenSettings { TokenExpirationMinutes = 60 }
            )
        );

        services.AddSingleton<TokenHeaderMiddleware>();
        services
            .AddHttpClient<ITokenService, TokenService>()
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl));

        services.AddResiliencePipeline<string, HttpResponseMessage>(
            "pipeline",
            (builder, context) =>
            {
                // You can access the service provider through the context
                var sp = context.ServiceProvider;
                builder.AddRetry(
                    new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 1,
                        BackoffType = DelayBackoffType.Linear,
                        Delay = TimeSpan.FromSeconds(1),
                        ShouldHandle = args =>
                            args.Outcome.Result
                                is {
                                    StatusCode: HttpStatusCode.Forbidden
                                        or HttpStatusCode.Unauthorized
                                }
                                ? PredicateResult.True()
                                : PredicateResult.False(),
                        OnRetry = async arguments =>
                        {
                            var cacheService = sp.GetRequiredService<ITokenService>();
                            await cacheService.GetTokenAsync(
                                arguments.Context.CancellationToken,
                                true
                            );
                        },
                    }
                );
            }
        );
        services
            .AddHttpClient<IBankAccountService, BankAccountService>()
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<TokenHeaderMiddleware>();

        var serviceProvider = services.BuildServiceProvider();
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        await cache.SetStringAsync(
            "ApiToken",
            "old-token",
            new DistributedCacheEntryOptions
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60),
            },
            CancellationToken.None
        );

        var bankAccountService = serviceProvider.GetRequiredService<IBankAccountService>();
        var bankAccountsResponse = await bankAccountService.ListBankAccountsAsync(
            CancellationToken.None
        );

        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
        var expected = JsonSerializer.Serialize(expectedBankAccountsResponse.BankAccounts);
        var actual = JsonSerializer.Serialize(bankAccountsResponse.BankAccounts);
        Assert.Equal(expected, actual);

        var capturedRequests = bankAccountResponseProvider.CapturedRequests;
        Assert.True(capturedRequests.Count == 2);

        // Assert whether "capturedRequests" each request's header contains the bearer tokens
        Assert.Equal("Bearer old-token", capturedRequests[0].Headers?["Authorization"].ToString());
        Assert.Equal("Bearer new-token", capturedRequests[1].Headers?["Authorization"].ToString());
    }
}
