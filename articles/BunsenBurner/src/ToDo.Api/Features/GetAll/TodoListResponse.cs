using ToDo.Api.Features.SearchById;

namespace ToDo.Api.Features.GetAll;

public record TodoListResponse
{
    public List<TodoResponse> Tasks { get; init; } = new();
}
