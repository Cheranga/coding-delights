using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Microsoft.Extensions.DependencyInjection;
using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services.Tests;

public partial class NewBankAccountServiceTests
{
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    [Fact]
    public async Task Test1()
    {
        var tokenResponseProvider = SetupTokenResponseProvider();

        var expectedBankAccountsResponse = new AutoFaker<ListBankAccountsResponse>().Generate();
        var bankAccountResponseProvider = SetupBankAccountResponseProvider(
            expectedBankAccountsResponse
        );

        var serviceProvider = RegisterServicesAndGetApplication();
        await SetCache(serviceProvider, "old-token");

        var bankAccountService = serviceProvider.GetRequiredService<IBankAccountService>();
        var actualBankAccountsResponse = await bankAccountService.ListBankAccountsAsync(
            CancellationToken.None
        );

        Assert.True(
            AssertExtensions.AreSame(expectedBankAccountsResponse, actualBankAccountsResponse)
        );

        var capturedRequests = bankAccountResponseProvider.CapturedRequests;
        Assert.True(capturedRequests.Count == 2);
        Assert.Equal("Bearer old-token", capturedRequests[0].Headers?["Authorization"].ToString());
        Assert.Equal("Bearer new-token", capturedRequests[1].Headers?["Authorization"].ToString());

        Assert.Single(tokenResponseProvider.CapturedRequests);
    }
}
