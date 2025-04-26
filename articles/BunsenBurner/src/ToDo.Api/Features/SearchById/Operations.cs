using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDo.Api.Features.SearchById;

internal static class Operations
{
    public static async ValueTask<Results<ProblemHttpResult, NoContent, Ok<TodoResponse>>> ExecuteAsync(
        [FromServices] IQueryHandler<SearchByIdQuery, TodoDataModel> queryHandler,
        [FromServices] ILogger<Program> logger,
        [FromRoute] string id,
        CancellationToken token = new()
    )
    {
        try
        {
            var task = await queryHandler.QueryAsync(new SearchByIdQuery(id), token);
            return task == null
                ? TypedResults.NoContent()
                : TypedResults.Ok(
                    new TodoResponse
                    {
                        Id = task.Id,
                        Title = task.Title,
                        Description = task.Description,
                        DueDate = task.DueDate,
                    }
                );
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "error occurred when searching for task");
        }

        return TypedResults.Problem(
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Search by Id",
                Detail = "error occurred when searching task by id",
            }
        );
    }
}
