using System.Net;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;
using Moq;
using Polly;
using ProtoBuf.Meta;
using ResilientHttpClients.Services.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace ResilientHttpClients.Services.Tests;

public partial class BankAccountServiceTests(WiremockFixture wiremockFixture) : IClassFixture<WiremockFixture>
{
    public WireMockServer Server => wiremockFixture.Server;

    private CustomResponseProvider SetupTokenResponseProvider()
    {
        var tokenResponseProvider = CustomResponseProvider.New(
            () => Response.Create().WithStatusCode(HttpStatusCode.OK).WithBody("new-token")
        );

        Server.Given(Request.Create().UsingGet().WithPath("/api/token")).RespondWith(tokenResponseProvider);

        return tokenResponseProvider;
    }

    private CustomResponseProvider SetupBankAccountResponseProvider(ListBankAccountsResponse expectedBankAccountsResponse)
    {
        var bankAccountResponseProvider = CustomResponseProvider.New(
            () => Response.Create().WithStatusCode(HttpStatusCode.Unauthorized),
            () => Response.Create().WithStatusCode(HttpStatusCode.OK).WithBodyAsJson(expectedBankAccountsResponse)
        );
        Server.Given(Request.Create().UsingGet().WithPath("/api/accounts")).RespondWith(bankAccountResponseProvider);

        return bankAccountResponseProvider;
    }

    private IServiceProvider RegisterServicesAndGetApplication()
    {
        var baseUrl = Server.Urls[0];
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton(
            Mock.Of<IOptionsMonitor<TokenSettings>>(builder =>
                builder.CurrentValue == new TokenSettings { TokenExpirationMinutes = 60 }
            )
        );

        services.AddResiliencePipeline<string, HttpResponseMessage>(
            "pipeline",
            (builder, context) =>
            {
                var sp = context.ServiceProvider;
                builder.AddRetry(
                    new HttpRetryStrategyOptions
                    {
                        MaxRetryAttempts = 1,
                        BackoffType = DelayBackoffType.Linear,
                        Delay = TimeSpan.FromSeconds(1),
                        ShouldHandle = args =>
                            args.Outcome.Result is { StatusCode: HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized }
                                ? PredicateResult.True()
                                : PredicateResult.False(),
                        OnRetry = async arguments =>
                        {
                            var cacheService = sp.GetRequiredService<ITokenService>();
                            await cacheService.GetTokenAsync(arguments.Context.CancellationToken, true);
                        },
                    }
                );
            }
        );

        services
            .AddHttpClient<ITokenService, TokenService>()
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl));

        services.AddSingleton<TokenHeaderMiddleware>();

        services
            .AddHttpClient<IBankAccountService, BankAccountService>()
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<TokenHeaderMiddleware>();
        return services.BuildServiceProvider();
    }

    private IServiceProvider RegisterServicesAndSetupOtherBankService()
    {
        var baseUrl = Server.Urls[0];
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton(
            Mock.Of<IOptionsMonitor<TokenSettings>>(builder =>
                builder.CurrentValue == new TokenSettings { TokenExpirationMinutes = 60 }
            )
        );

        services
            .AddHttpClient<ITokenService, TokenService>()
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl));

        services.AddSingleton<TokenHeaderMiddleware>();

        services
            .AddHttpClient<IBankAccountService, OtherBankAccountService>()
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<TokenHeaderMiddleware>()
            .AddResilienceHandler(
                "pipeline",
                (builder, context) =>
                {
                    var sp = context.ServiceProvider;
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
                            OnRetry = async arguments =>
                            {
                                var cacheService = sp.GetRequiredService<ITokenService>();
                                await cacheService.GetTokenAsync(arguments.Context.CancellationToken, true);
                            },
                        }
                    );
                }
            );
        return services.BuildServiceProvider();
    }

    private static Task SetCache(IServiceProvider serviceProvider, string token)
    {
        var cache = serviceProvider.GetRequiredService<IDistributedCache>();

        return cache.SetStringAsync(
            "ApiToken",
            token,
            new DistributedCacheEntryOptions { AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(60) },
            CancellationToken.None
        );
    }
}
