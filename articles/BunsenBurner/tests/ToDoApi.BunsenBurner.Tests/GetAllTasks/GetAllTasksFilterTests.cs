using System.Net;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Infrastructure.DataAccess;
using static BunsenBurner.Bdd;

namespace ToDoApi.BunsenBurner.Tests.GetAllTasks;

public partial class GetAllTasksFilterTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "Given tasks are cached, when get all endpoint is called, then must return tasks from the cache")]
    public async Task GetAllTasksWhenCached() =>
        await Given(() =>
            {
                var fixture = new Fixture();
                return GetMockedQueryHandler(
                    [],
                    fixture.CreateMany<TodoDataModel>(5).ToList(),
                    fixture.CreateMany<TodoDataModel>(10).ToList()
                );
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
            {
                responses.httpResponse1.StatusCode.Should().Be(HttpStatusCode.NoContent);
                responses.httpResponse2.StatusCode.Should().Be(HttpStatusCode.OK);
                responses.httpResponse3.StatusCode.Should().Be(HttpStatusCode.OK);
            })
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
                todoListResponse.Should().NotBeNull();
                todoListResponse!.Tasks.Should().NotBeNull().And.HaveCount(5);
            })
            .And(async responses =>
            {
                var todoListResponse = await GetToDoListFromResponse(responses.httpResponse3);
                todoListResponse.Should().NotBeNull();
                todoListResponse!.Tasks.Should().NotBeNull().And.HaveCount(5);
            });
}
