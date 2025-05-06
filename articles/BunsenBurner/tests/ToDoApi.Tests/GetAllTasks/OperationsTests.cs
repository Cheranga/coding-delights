using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Moq;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDoApi.Tests.GetAllTasks;

public static class OperationsTests
{
    [Fact(DisplayName = "Cache only if tasks are in the database")]
    public static async Task CacheOnlyIfTasksAreAvailable()
    {
        // Arrange
        var mockedCache = new Mock<IDistributedCache>();

        var mockedQueryHandler = new Mock<IQueryHandler<SearchAllQuery, List<TodoDataModel>>>();
        mockedQueryHandler.Setup(x => x.QueryAsync(It.IsAny<SearchAllQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync([]);

        // Act
        var response = await Operations.ExecuteAsync(mockedCache.Object, mockedQueryHandler.Object, Mock.Of<ILogger<Program>>());

        // Assert
        mockedCache.Verify(
            x =>
                x.SetAsync(Constants.CacheKey, It.IsAny<byte[]>(), It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()),
            Times.Never()
        );

        Assert.True(response.Result.GetType() == typeof(NoContent));
    }
}
