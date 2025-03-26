using System.Net;
using System.Net.Http.Json;
using Bogus;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Registry;
using ResilientHttpClients.Services.Models;
using WireMock.RequestBuilders;
using WireMock.ResponseBuilders;
using WireMock.Server;

namespace TestProject1;

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TokenHeaderMiddleware>();
        services
            .AddHttpClient("test")
            .ConfigureHttpClient(builder =>
                builder.BaseAddress = new Uri("https://api.github.com/users")
            )
            .AddHttpMessageHandler<TokenHeaderMiddleware>();

        var httpResponse = await services
            .BuildServiceProvider()
            .GetRequiredService<IHttpClientFactory>()
            .CreateClient("test")
            .GetAsync("/octocat");
    }

    [Fact]
    public async Task Test2()
    {
        var baseUrl = "https://api.github.com/users";
        var services = new ServiceCollection();
        services.AddSingleton<TokenHeaderMiddleware>();
        services
            .AddHttpClient("test")
            .ConfigureHttpClient(builder => builder.BaseAddress = new Uri(baseUrl))
            .AddHttpMessageHandler<TokenHeaderMiddleware>();

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
                    }
                );
            }
        );
        var sp = services.BuildServiceProvider();
        var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient("test");

        var count = 0;

        var pipelineProvider = sp.GetRequiredService<ResiliencePipelineProvider<string>>();
        var pipeline = pipelineProvider.GetPipeline<HttpResponseMessage>("pipeline");
        var operation = await pipeline.ExecuteAsync<HttpResponseMessage>(async _ =>
        {
            count++;
            var response = await httpClient.GetAsync("/cheranga");
            return response;
        });

        Assert.True(count == 2);
    }

    [Fact]
    public async Task Test3()
    {
        var wiremockServer = WireMockServer.Start();
        var baseUrl = wiremockServer.Urls[0];

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/token"))
            .InScenario(0)
            .WillSetStateTo(1)
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new TokenResponse { Token = "old-token" })
            );

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/token"))
            .InScenario(0)
            .WhenStateIs(1)
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new TokenResponse { Token = "new-token" })
            );

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/orders"))
            .InScenario(1)
            .WillSetStateTo(1)
            .RespondWith(Response.Create().WithStatusCode(HttpStatusCode.Unauthorized));

        var bankAccounts = new Faker<BankAccountResponse>().Generate(3);

        wiremockServer
            .Given(Request.Create().UsingGet().WithPath("/api/orders"))
            .InScenario(1)
            .WhenStateIs(1)
            .RespondWith(
                Response
                    .Create()
                    .WithStatusCode(200)
                    .WithBodyAsJson(new ListBankAccountsResponse { BankAccounts = bankAccounts })
            );

        var services = new ServiceCollection();
    }
}

public class OrderService(
    HttpClient httpClient,
    ResiliencePipelineProvider<string> pipelineProvider
)
{
    public async Task<ListBankAccountsResponse?> GetBankAccountsAsync()
    {
        var pipeline = pipelineProvider.GetPipeline<HttpResponseMessage>("pipeline");
        var response = await pipeline.ExecuteAsync<HttpResponseMessage>(async ct =>
        {
            var response = await httpClient.GetAsync("/api/orders", ct);
            return response;
        });

        if (response.StatusCode == HttpStatusCode.Unauthorized)
        {
            throw new UnauthorizedAccessException();
        }

        var bankAccountsResponse =
            await response.Content.ReadFromJsonAsync<ListBankAccountsResponse>();
        return bankAccountsResponse;
    }
}
