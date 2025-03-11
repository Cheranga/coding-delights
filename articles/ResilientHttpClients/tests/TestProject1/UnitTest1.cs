using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Polly;
using Polly.Registry;
using Polly.Retry;

namespace TestProject1;

public class TokenHeaderMiddleware : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        // Add token header if it's not already present
        request.Headers.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            Guid.NewGuid().ToString("N")
        );
        return await base.SendAsync(request, cancellationToken);
    }
}

public class ResilienceHandlerMiddleware : DelegatingHandler
{
    // For example, a Polly retry policy that retries once on 401 Unauthorized.
    // private readonly AsyncRetryPolicy<HttpResponseMessage> _retryPolicy = Policy
    //     .HandleResult<HttpResponseMessage>(r => r.StatusCode == System.Net.HttpStatusCode.Forbidden)
    //     .RetryAsync(1);

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        return base.SendAsync(request, cancellationToken);
        // return await _retryPolicy.ExecuteAsync(async (ct) =>
        // {
        //     // Clone the request for each execution to re-trigger the pipeline.
        //     var clonedRequest = await request.CloneAsync();
        //     return await base.SendAsync(clonedRequest, ct);
        // }, cancellationToken);
    }
}

public class UnitTest1
{
    [Fact]
    public async Task Test1()
    {
        var services = new ServiceCollection();
        services.AddSingleton<TokenHeaderMiddleware>();
        services.AddSingleton<ResilienceHandlerMiddleware>();
        services
            .AddHttpClient("test")
            .ConfigureHttpClient(builder =>
                builder.BaseAddress = new Uri("https://api.github.com/users")
            )
            .AddHttpMessageHandler<TokenHeaderMiddleware>()
            .AddHttpMessageHandler<ResilienceHandlerMiddleware>();

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
        services.AddSingleton<ResilienceHandlerMiddleware>();
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
}
