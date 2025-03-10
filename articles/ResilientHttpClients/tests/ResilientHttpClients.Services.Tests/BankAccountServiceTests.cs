using System.Text;
using Microsoft.Extensions.Caching.Distributed;
using Moq;

namespace ResilientHttpClients.Services.Tests;

public partial class BankAccountServiceTests
{
    [Fact]
    public async Task Test1() =>
        await Arrange(() =>
            {
                SetupTokenRequestResponses(_wireMockServer);
                SetupOrdersRequestResponses(_wireMockServer);
                SetupMockedCache(_mockedCache);

                return GetBankAccountService();
            })
            .Act(async data => await data.ListBankAccountsAsync(CancellationToken.None))
            .Assert(result => result.BankAccounts.Count == 1)
            .And(
                (_, _) =>
                    _mockedCache.Verify(
                        x => x.GetAsync("ApiToken", It.IsAny<CancellationToken>()),
                        Times.Once
                    )
            )
            .And(
                (_, _) =>
                {
                    _mockedCache.Verify(
                        x =>
                            x.SetAsync(
                                "ApiToken",
                                Encoding.UTF8.GetBytes("new-token"),
                                It.IsAny<DistributedCacheEntryOptions>(),
                                It.IsAny<CancellationToken>()
                            ),
                        Times.Once
                    );
                }
            );
}
