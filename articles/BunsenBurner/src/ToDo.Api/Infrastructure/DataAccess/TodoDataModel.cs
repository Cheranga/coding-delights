namespace ToDo.Api.Infrastructure.DataAccess;

public record TodoDataModel(string Id, string Title, string Description, DateTimeOffset DueDate);
