namespace ToDo.Api.Features.SearchById;

public record TodoResponse
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset DueDate { get; init; }
}
