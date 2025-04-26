using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Distributed;
using ToDo.Api.Features.SearchById;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDo.Api.Features.GetAll;

internal static class Operations
{
    public static async ValueTask<Results<ProblemHttpResult, NoContent, Ok<TodoListResponse>>> ExecuteAsync(
        [FromServices] IDistributedCache cache,
        [FromServices] IQueryHandler<SearchAllQuery, List<TodoDataModel>> queryHandler,
        [FromServices] ILogger<Program> logger,
        CancellationToken token = new()
    )
    {
        try
        {
            var tasks = await queryHandler.QueryAsync(new SearchAllQuery(), token) ?? new List<TodoDataModel>();
            if (!tasks.Any())
                return TypedResults.NoContent();

            await CacheTasks(cache, tasks, token);

            return TypedResults.Ok(
                new TodoListResponse
                {
                    Tasks =
                    [
                        .. tasks
                            .Select(x => new TodoResponse
                            {
                                Id = x.Id,
                                Title = x.Title,
                                Description = x.Description,
                                DueDate = x.DueDate,
                            })
                            .ToList(),
                    ],
                }
            );
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "error occurred when getting all the tasks");
        }

        return TypedResults.Problem(
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Get all tasks",
                Detail = "error occurred when getting all tasks",
            }
        );
    }

    private static async ValueTask CacheTasks(IDistributedCache cache, List<TodoDataModel> tasks, CancellationToken token)
    {
        using var stream = new MemoryStream();
        await JsonSerializer.SerializeAsync(stream, tasks, Constants.SerializerOptions, token);
        stream.Position = 0;
        using var reader = new BinaryReader(stream);
        var bytes = reader.ReadBytes((int)stream.Length);
        await cache.SetAsync(Constants.CacheKey, bytes, Constants.CacheOptions, token);
    }
}
