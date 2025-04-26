using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using ToDo.Api.Features.SearchById;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDo.Api.Features.GetAll;

internal class GetAllTasksFilter(IDistributedCache cache) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var tasks = new List<TodoDataModel>();
        var rawData = await cache.GetAsync(Constants.CacheKey);
        if (rawData is { Length: > 0 })
        {
            using var memoryStream = new MemoryStream(rawData);
            tasks = await JsonSerializer.DeserializeAsync<List<TodoDataModel>>(memoryStream, Constants.SerializerOptions);
        }

        if (tasks is { Count: > 0 })
        {
            return TypedResults.Ok(
                new TodoListResponse
                {
                    Tasks =
                    [
                        .. tasks.Select(x => new TodoResponse
                        {
                            Id = x.Id,
                            Title = x.Title,
                            Description = x.Description,
                            DueDate = x.DueDate,
                        }),
                    ],
                }
            );
        }

        return await next(context);
    }
}
