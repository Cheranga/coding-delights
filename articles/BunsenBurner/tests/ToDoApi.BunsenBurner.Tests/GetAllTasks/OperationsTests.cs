using AutoBogus;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Infrastructure.DataAccess;
using static BunsenBurner.ArrangeActAssert;

namespace ToDoApi.BunsenBurner.Tests.GetAllTasks;

public static class OperationsTests
{
    [Fact(DisplayName = "Cache only if tasks are in the database")]
    public static async Task CacheOnlyIfTasksAreAvailable() =>
        await Arrange(() =>
            {
                var mockedCache = new Mock<IDistributedCache>();

                var mockedQueryHandler = new Mock<IQueryHandler<SearchAllQuery, List<TodoDataModel>>>();
                mockedQueryHandler
                    .Setup(x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync([]);

                return (mockedCache, mockedQueryHandler);
            })
            .Act(async data =>
                await Operations.ExecuteAsync(
                    data.mockedCache.Object,
                    data.mockedQueryHandler.Object,
                    Mock.Of<ILogger<Program>>()
                )
            )
            .Assert(
                (data, _) =>
                {
                    data.mockedCache.Verify(
                        x =>
                            x.SetAsync(
                                Constants.CacheKey,
                                It.IsAny<byte[]>(),
                                It.IsAny<DistributedCacheEntryOptions>(),
                                It.IsAny<CancellationToken>()
                            ),
                        Times.Never
                    );
                }
            )
            .And(response => response.Result.GetType() == typeof(NoContent));

    [Fact(DisplayName = "When tasks are available, it will be cached")]
    public static async Task TasksAreAvailableAndWillBeCached() =>
        await Arrange(() =>
            {
                var mockedCache = new Mock<IDistributedCache>();

                var tasks = new AutoFaker<TodoDataModel>().Generate(3);
                var mockedQueryHandler = new Mock<IQueryHandler<SearchAllQuery, List<TodoDataModel>>>();
                mockedQueryHandler
                    .Setup(x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync(tasks);

                return (mockedCache, mockedQueryHandler);
            })
            .Act(async data =>
                await Operations.ExecuteAsync(
                    data.mockedCache.Object,
                    data.mockedQueryHandler.Object,
                    Mock.Of<ILogger<Program>>()
                )
            )
            .Assert(
                (data, _) =>
                {
                    data.mockedCache.Verify(
                        x =>
                            x.SetAsync(
                                Constants.CacheKey,
                                It.IsAny<byte[]>(),
                                It.IsAny<DistributedCacheEntryOptions>(),
                                It.IsAny<CancellationToken>()
                            ),
                        Times.Once
                    );
                }
            )
            .And(response =>
            {
                var todoListResponse = response.Result switch
                {
                    Ok<TodoListResponse> r => r.Value,
                    _ => null,
                };
                Assert.NotNull(todoListResponse!.Tasks);
                Assert.Equal(3, todoListResponse.Tasks.Count);
            });

    [Fact(DisplayName = "If error occurs when getting tasks from database, then must return problem response")]
    public static async Task ErrorWhenGettingTasks() =>
        await Arrange(() =>
            {
                var mockedQueryHandler = new Mock<IQueryHandler<SearchAllQuery, List<TodoDataModel>>>();
                mockedQueryHandler
                    .Setup(x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("error!"));

                var mockedCache = new Mock<IDistributedCache>();

                return (mockedCache, mockedQueryHandler);
            })
            .Act(async data =>
                await Operations.ExecuteAsync(
                    data.mockedCache.Object,
                    data.mockedQueryHandler.Object,
                    Mock.Of<ILogger<Program>>()
                )
            )
            .Assert(
                (data, _) =>
                {
                    data.mockedCache.Verify(
                        x =>
                            x.SetAsync(
                                Constants.CacheKey,
                                It.IsAny<byte[]>(),
                                It.IsAny<DistributedCacheEntryOptions>(),
                                It.IsAny<CancellationToken>()
                            ),
                        Times.Never
                    );
                }
            )
            .And(response =>
            {
                var problemDetails = response.Result switch
                {
                    ProblemHttpResult p => p.ProblemDetails,
                    _ => null,
                };

                Assert.Equal("error occurred when getting all tasks", problemDetails!.Detail);
            });
}
