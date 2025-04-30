using AutoBogus;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using ToDo.Api.Features.SearchById;
using ToDo.Api.Infrastructure.DataAccess;
using static BunsenBurner.ArrangeActAssert;

namespace ToDoApi.BunsenBurner.Tests.SearchById;

public static class OperationsTests
{
    [Fact(DisplayName = "There is no task for the requested task id")]
    public static async Task NoTaskForProvidedId() =>
        await Arrange(() =>
            {
                TodoDataModel? dataModel = null;
                var mockedQueryHandler = new Mock<IQueryHandler<SearchByIdQuery, TodoDataModel>>();
                mockedQueryHandler
                    .Setup(x => x.QueryAsync(It.IsAny<SearchByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(dataModel);

                return mockedQueryHandler;
            })
            .Act(async qh =>
                await Operations.ExecuteAsync(qh.Object, Mock.Of<ILogger<Program>>(), "666", It.IsAny<CancellationToken>())
            )
            .Assert(response => response.Result.GetType() == typeof(NoContent));

    [Fact(DisplayName = "Task is available for the requested task id")]
    public static async Task TaskIsAvailable() =>
        await Arrange(() =>
            {
                var mockedQueryHandler = new Mock<IQueryHandler<SearchByIdQuery, TodoDataModel>>();
                mockedQueryHandler
                    .Setup(x => x.QueryAsync(It.IsAny<SearchByIdQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(new AutoFaker<TodoDataModel>().Generate());

                return mockedQueryHandler;
            })
            .Act(async qh =>
                await Operations.ExecuteAsync(qh.Object, Mock.Of<ILogger<Program>>(), "666", It.IsAny<CancellationToken>())
            )
            .Assert(response =>
            {
                var todoResponse = response.Result switch
                {
                    Ok<TodoResponse> r => r.Value,
                    _ => null,
                };
                Assert.NotNull(todoResponse);
            });

    [Fact(DisplayName = "When searching for task by id, an error occurs")]
    public static async Task ErrorWhenGettingTaskFromDatabase() =>
        await Arrange(() =>
            {
                var mockedQueryHandler = new Mock<IQueryHandler<SearchByIdQuery, TodoDataModel>>();
                mockedQueryHandler
                    .Setup(x => x.QueryAsync(It.IsAny<SearchByIdQuery>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("error!"));

                return mockedQueryHandler;
            })
            .Act(async qh =>
                await Operations.ExecuteAsync(qh.Object, Mock.Of<ILogger<Program>>(), "666", It.IsAny<CancellationToken>())
            )
            .Assert(response =>
            {
                var problemDetails = response.Result switch
                {
                    ProblemHttpResult p => p.ProblemDetails,
                    _ => null,
                };
                Assert.Equal("error occurred when searching task by id", problemDetails!.Detail);
            });
}