using ToDo.Api.Features.SearchById;

namespace ToDo.Api.Features.GetAll;

public record TodoListResponse
{
    public IReadOnlyList<TodoResponse> Tasks { get; init; } = [];
}
