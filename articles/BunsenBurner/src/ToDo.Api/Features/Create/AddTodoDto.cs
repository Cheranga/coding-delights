using System.Diagnostics.CodeAnalysis;

namespace ToDo.Api.Features.Create;

[ExcludeFromCodeCoverage]
public record AddTodoDto
{
    public string Title { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public DateTimeOffset DueDate { get; init; }
}
