using System.Net;
using AutoBogus;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDoApi.Tests.GetAllTasks;

public partial class GetAllTasksFilterTests(WebApplicationFactory<Program> factory)
    : IClassFixture<WebApplicationFactory<Program>>
{
    [Fact(DisplayName = "Given tasks are cached, when get all endpoint is called, then must return tasks from the cache")]
    public async Task GetAllTasksWhenCached()
    {
        // Given
        var fixture = new AutoFaker<TodoDataModel>();
        var mockedQueryHandler = GetMockedQueryHandler([], fixture.Generate(5), fixture.Generate(10));
        var httpClient = factory
            .WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    services.AddSingleton(mockedQueryHandler.Object);
                });
            })
            .CreateClient();

        // When
        // Call the endpoint three times
        var httpResponse1 = await httpClient.GetAsync("/todos");
        var httpResponse2 = await httpClient.GetAsync("/todos");
        var httpResponse3 = await httpClient.GetAsync("/todos");

        // Then
        // The query handler must be called only two times, as the third response must be taken from the cache
        mockedQueryHandler.Verify(x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
        // The first response must be 204 No Content
        // The second and third responses must be 200 OK
        Assert.Equal(HttpStatusCode.NoContent, httpResponse1.StatusCode);
        Assert.Equal(HttpStatusCode.OK, httpResponse2.StatusCode);
        Assert.Equal(HttpStatusCode.OK, httpResponse3.StatusCode);

        var todoListResponse = await GetToDoListResponse(httpResponse2);
        Assert.NotNull(todoListResponse);
        Assert.NotNull(todoListResponse.Tasks);
        Assert.Equal(5, todoListResponse.Tasks.Count);

        todoListResponse = await GetToDoListResponse(httpResponse3);
        Assert.NotNull(todoListResponse);
        Assert.NotNull(todoListResponse.Tasks);
        Assert.Equal(5, todoListResponse.Tasks.Count);
    }
}
