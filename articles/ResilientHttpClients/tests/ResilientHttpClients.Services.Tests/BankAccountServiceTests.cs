using System.Net;
using Microsoft.Extensions.DependencyInjection;
using RichardSzalay.MockHttp;

namespace ResilientHttpClients.Services.Tests;

public class BankAccountServiceTests
{
    [Fact]
    public async Task Test1()
    {
        var services = new ServiceCollection();
        services.AddDistributedMemoryCache();
        services.AddSingleton<TokenHeaderMiddleware>();

        var mockedTokenHttp = new MockHttpMessageHandler();
        mockedTokenHttp
            .When(HttpMethod.Get, "https://blah.com/api/token")
            .Respond(HttpStatusCode.OK, new StringContent("old-token"));
        mockedTokenHttp
            .When(HttpMethod.Get, "https://blah.com/api/token")
            .Respond(HttpStatusCode.OK, new StringContent("new-token"));
        services
            .AddHttpClient<ITokenService, TokenService>()
            .ConfigurePrimaryHttpMessageHandler(() => mockedTokenHttp);

        services
            .AddHttpClient<IBankAccountService, BankAccountService>()
            .AddHttpMessageHandler<TokenHeaderMiddleware>();
    }
}
