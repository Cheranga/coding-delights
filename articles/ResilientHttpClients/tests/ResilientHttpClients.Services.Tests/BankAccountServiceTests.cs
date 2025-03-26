// using System.Net;
// using System.Text;
// using Bogus;
// using Microsoft.Extensions.Caching.Distributed;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Http.Resilience;
// using Microsoft.Extensions.Options;
// using Moq;
// using Polly;
// using Polly.Registry;
// using ResilientHttpClients.Services.Models;
// using WireMock.RequestBuilders;
// using WireMock.ResponseBuilders;
// using WireMock.Server;
//
// namespace ResilientHttpClients.Services.Tests;
//
// public partial class BankAccountServiceTests
// {
//     [Fact]
//     public async Task Test1() =>
//         await Arrange(() =>
//             {
//                 SetupTokenRequestResponses(_wireMockServer);
//                 SetupOrdersRequestResponses(_wireMockServer);
//                 SetupMockedCache(_mockedCache);
//
//                 return GetBankAccountService();
//             })
//             .Act(async data => await data.ListBankAccountsAsync(CancellationToken.None))
//             .Assert(result => result.BankAccounts.Count == 1)
//             .And(
//                 (_, _) =>
//                     _mockedCache.Verify(
//                         x => x.GetAsync("ApiToken", It.IsAny<CancellationToken>()),
//                         Times.Once
//                     )
//             )
//             .And(
//                 (_, _) =>
//                 {
//                     _mockedCache.Verify(
//                         x =>
//                             x.SetAsync(
//                                 "ApiToken",
//                                 Encoding.UTF8.GetBytes("new-token"),
//                                 It.IsAny<DistributedCacheEntryOptions>(),
//                                 It.IsAny<CancellationToken>()
//                             ),
//                         Times.Once
//                     );
//                 }
//             );
//
//
// }
