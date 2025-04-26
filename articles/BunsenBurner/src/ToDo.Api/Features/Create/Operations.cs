using System.Net;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using ToDo.Api.Features.SearchById;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDo.Api.Features.Create;

internal static class Operations
{
    private static CreateToDoCommand ToCommand(this AddTodoDto dto) => new(dto.Title, dto.Description, dto.DueDate);

    private static TodoResponse ToResponse(this AddTodoDto dto, string id) =>
        new()
        {
            Id = id,
            Title = dto.Title,
            Description = dto.Description,
            DueDate = dto.DueDate,
        };

    public static async ValueTask<Results<ProblemHttpResult, Created<TodoResponse>>> ExecuteAsync(
        [FromServices] ICommandHandler<CreateToDoCommand> commandHandler,
        [FromServices] ILogger<Program> logger,
        [FromBody] AddTodoDto dto,
        CancellationToken token = new()
    )
    {
        try
        {
            var command = dto.ToCommand();
            var createdId = await commandHandler.ExecuteAsync(command, token);
            var response = dto.ToResponse(createdId);

            return TypedResults.Created($"{RouteRegistrar.TodoPrefix}/{response.Id}", response);
        }
        catch (Exception exception)
        {
            logger.LogWarning(exception, "error occurred when adding task");
        }

        return TypedResults.Problem(
            new ProblemDetails
            {
                Status = (int)HttpStatusCode.InternalServerError,
                Title = "Error when adding task",
                Detail = "error occurred when adding a task",
            }
        );
    }
}
