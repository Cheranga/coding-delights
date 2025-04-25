using AutoBogus;
using Microsoft.Extensions.DependencyInjection;
using ResilientHttpClients.Services.Models;

namespace ResilientHttpClients.Services.Tests;

public partial class BankAccountServiceTests
{
    [Fact]
    public async Task Test1()
    {
        await Arrange(() =>
            {
                var tokenResponseProvider = SetupTokenResponseProvider();
                return tokenResponseProvider;
            })
            .And(data =>
            {
                var expectedBankAccountsResponse = new AutoFaker<ListBankAccountsResponse>().Generate();
                var bankAccountResponseProvider = SetupBankAccountResponseProvider(expectedBankAccountsResponse);

                return (tokenResponseProvider: data, bankAccountResponseProvider, expectedBankAccountsResponse);
            })
            .And(data =>
            {
                var serviceProvider = RegisterServicesAndGetApplication();
                return (
                    data.tokenResponseProvider,
                    data.bankAccountResponseProvider,
                    data.expectedBankAccountsResponse,
                    serviceProvider
                );
            })
            .And(async data =>
            {
                await SetCache(data.serviceProvider, "old-token");
                return data;
            })
            .Act(async data =>
            {
                var bankService = data.serviceProvider.GetRequiredService<IBankAccountService>();
                return await bankService.ListBankAccountsAsync(CancellationToken.None);
            })
            .Assert((data, response) => AssertExtensions.AreSame(data.expectedBankAccountsResponse, response))
            .And((data, _) => data.tokenResponseProvider.CapturedRequests.Count == 1)
            .And((data, _) => data.bankAccountResponseProvider.CapturedRequests.Count == 2)
            .And(
                (data, _) =>
                {
                    var capturedRequests = data.bankAccountResponseProvider.CapturedRequests;
                    Assert.True(capturedRequests.Count == 2);
                    Assert.Equal("Bearer old-token", capturedRequests[0].Headers?["Authorization"].ToString());
                    Assert.Equal("Bearer new-token", capturedRequests[1].Headers?["Authorization"].ToString());
                }
            );
    }
}
