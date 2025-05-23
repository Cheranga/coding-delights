using System.Text.Json;
using Moq;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDoApi.Tests.GetAllTasks;

public partial class GetAllTasksFilterTests
{
    private static Mock<IQueryHandler<SearchAllQuery, List<TodoDataModel>>> GetMockedQueryHandler(params List<TodoDataModel>[] results)
    {
        var mockedQueryHandler = new Mock<IQueryHandler<SearchAllQuery, List<TodoDataModel>>>();
        var setupAction = mockedQueryHandler.SetupSequence(x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>()));
        results.ToList().ForEach(x => setupAction.ReturnsAsync(x));

        return mockedQueryHandler;
    }

    private static async Task<TodoListResponse?> GetToDoListResponse(HttpResponseMessage httpResponse)
    {
        var responseContent = await httpResponse.Content.ReadAsStringAsync();
        var todoListResponse = JsonSerializer.Deserialize<TodoListResponse>(responseContent, Constants.SerializerOptions);
        return todoListResponse;
    }
}
