// using System.Net;
// using System.Net.Http.Json;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Http.Resilience;
// using Moq;
// using Polly;
// using ResilientHttpClients.Services.Models;
// using RichardSzalay.MockHttp;
// using WireMock.RequestBuilders;
// using WireMock.ResponseBuilders;
// using WireMock.Server;
//
// namespace ResilientHttpClients.Services.Tests;
//
// public sealed class MockTest
// {
//     [Fact]
//     public async Task Test1()
//     {
//         var baseUrl = "https://localhost:3000";
//         var capturedRequests = new List<HttpRequestMessage>();
//         var mockHttp = new MockHttpMessageHandler();
//         mockHttp
//             .When(HttpMethod.Get, $"{baseUrl}/api/token")
//             .Respond(
//                 HttpStatusCode.OK,
//                 req =>
//                 {
//                     capturedRequests.Add(req);
//                     return JsonContent.Create(new TokenResponse {Token = "old-token"});
//                 }
//             );
//
//         mockHttp
//             .When(HttpMethod.Get, $"{baseUrl}/api/token")
//             .Respond(
//                 HttpStatusCode.OK,
//                 req =>
//                 {
//                     capturedRequests.Add(req);
//                     return JsonContent.Create(new TokenResponse {Token = "new-token"});
//                 }
//             );
//
//         var client = mockHttp.ToHttpClient();
//         var response1 = await client.GetFromJsonAsync<TokenResponse>($"{baseUrl}/api/token");
//         var response2 = await client.GetFromJsonAsync<TokenResponse>($"{baseUrl}/api/token");
//
//         Assert.Equal("old-token", response1?.Token);
//         Assert.Equal("new-token", response2?.Token);
//     }
//
//     [Fact]
//     public async Task Test2()
//     {
//         var baseUrl = "https://localhost:3000";
//         var testHttpHandler = new TestHttpHandler(
//             new Queue<Func<HttpResponseMessage>>(
//                 [
//                     () =>
//                         new HttpResponseMessage(HttpStatusCode.OK)
//                         {
//                             Content = JsonContent.Create(new TokenResponse {Token = "old-token"})
//                         },
//                     () =>
//                         new HttpResponseMessage(HttpStatusCode.OK)
//                         {
//                             Content = JsonContent.Create(new TokenResponse {Token = "new-token"})
//                         }
//                 ]
//             )
//         );
//         var httpClient = new HttpClient(testHttpHandler) {BaseAddress = new Uri(baseUrl)};
//
//         var response1 = await httpClient.GetFromJsonAsync<TokenResponse>("/api/token");
//         var response2 = await httpClient.GetFromJsonAsync<TokenResponse>("/api/token");
//     }
//
//     [Fact]
//     public async Task Test3()
//     {
//         var wiremockServer = WireMockServer.Start();
//
//         var customResponseProvider = CustomResponseProvider.New(
//             () => Response.Create().WithStatusCode(HttpStatusCode.Unauthorized),
//             () => Response.Create().WithStatusCode(HttpStatusCode.OK)
//         );
//
//         wiremockServer
//             .Given(Request.Create().UsingGet().WithPath("/api/token"))
//             .RespondWith(customResponseProvider);
//
//         var mockedTokenService = new Mock<ITokenService>();
//         mockedTokenService
//             .SetupSequence(x => x.GetTokenAsync(It.IsAny<CancellationToken>(), It.IsAny<bool>()))
//             .ReturnsAsync(new TokenResponse {Token = "old-token"})
//             .ReturnsAsync(new TokenResponse {Token = "new-token"});
//
//         var services = new ServiceCollection();
//         services.AddSingleton(mockedTokenService.Object);
//         services.AddSingleton<TokenHeaderMiddleware>();
//
//         services
//             .AddHttpClient("test")
//             .ConfigureHttpClient(builder =>
//                 builder.BaseAddress = new Uri("https://api.github.com/users")
//             )
//             .AddHttpMessageHandler<TokenHeaderMiddleware>()
//             .AddResilienceHandler(
//                 "test-pipeline",
//                 builder =>
//                 {
//                     builder.AddRetry(
//                         new HttpRetryStrategyOptions
//                         {
//                             MaxRetryAttempts = 1,
//                             BackoffType = DelayBackoffType.Linear,
//                             Delay = TimeSpan.FromSeconds(1),
//                             ShouldHandle = args =>
//                             {
//                                 var r = args.Context.GetRequestMessage();
//                                 return
//                                     args.Outcome.Result is {StatusCode: HttpStatusCode.Forbidden}
//                                         ? PredicateResult.True()
//                                         : PredicateResult.False();
//                             }
//                         }
//                     );
//                 }
//             );
//
//         var httpClient = services
//             .BuildServiceProvider()
//             .GetRequiredService<IHttpClientFactory>()
//             .CreateClient("test");
//
//         var response1 = await httpClient.GetAsync("/octocat");
//
//         // extract headers from CapturedRequests in customResponseProvider as a dictionary mapped by key and values
//         Assert.Equal(
//             "Bearer old-token",
//             customResponseProvider.CapturedRequests[0].Headers?["Authorization"].Single()
//         );
//         Assert.Equal(
//             "Bearer new-token",
//             customResponseProvider.CapturedRequests[1].Headers?["Authorization"].Single()
//         );
//     }
// }
