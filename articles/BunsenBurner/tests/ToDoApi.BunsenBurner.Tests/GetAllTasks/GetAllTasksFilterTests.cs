using System.Net;
using AutoBogus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Infrastructure.DataAccess;
using static BunsenBurner.GivenWhenThen;

namespace ToDoApi.BunsenBurner.Tests.GetAllTasks;

public partial class GetAllTasksFilterTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "Given tasks are cached, when get all endpoint is called, then must return tasks from the cache")]
    public async Task GetAllTasksWhenCached() =>
        await Given(() =>
            {
                var fixture = new AutoFaker<TodoDataModel>();
                return GetMockedQueryHandler([], fixture.Generate(5).ToList(), fixture.Generate(10).ToList());
            })
            .And(qh =>
            {
                var client = factory
                    .WithWebHostBuilder(builder =>
                    {
                        builder.ConfigureTestServices(services =>
                        {
                            services.AddSingleton(qh.Object);
                        });
                    })
                    .CreateClient();

                return (queryHandler: qh, client);
            })
            .When(async data =>
            {
                var httpResponse1 = await GetAllTasks(data.client);
                return httpResponse1;
            })
            .And(
                async (data, httpResponse1) =>
                {
                    var httpResponse2 = await GetAllTasks(data.client);
                    return (httpResponse1, httpResponse2);
                }
            )
            .And(
                async (data, responses) =>
                {
                    var httpResponse3 = await GetAllTasks(data.client);
                    return (responses.httpResponse1, responses.httpResponse2, httpResponse3);
                }
            )
            .Then(responses =>
                responses.httpResponse1.StatusCode == HttpStatusCode.NoContent
                && responses.httpResponse2.StatusCode == HttpStatusCode.OK
                && responses.httpResponse3.StatusCode == HttpStatusCode.OK
            )
            .And(
                (data, _) =>
                {
                    data.queryHandler.Verify(
                        x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>()),
                        Times.Exactly(2)
                    );
                }
            )
            .And(async responses =>
            {
                var todoListResponse = await GetToDoListFromResponse(responses.httpResponse2);
                Assert.NotNull(todoListResponse);
                Assert.Equal(5, todoListResponse.Tasks.Count);
            })
            .And(async responses =>
            {
                var todoListResponse = await GetToDoListFromResponse(responses.httpResponse3);
                Assert.NotNull(todoListResponse);
                Assert.Equal(5, todoListResponse.Tasks.Count);
            });
}
