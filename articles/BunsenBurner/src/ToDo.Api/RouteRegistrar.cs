using ToDo.Api.Features.Create;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Filters;
using Create = ToDo.Api.Features.Create.Operations;
using GetAll = ToDo.Api.Features.GetAll.Operations;
using SearchById = ToDo.Api.Features.SearchById.Operations;

namespace ToDo.Api;

internal static class RouteRegistrar
{
    internal const string TodoPrefix = "/todos";

    internal static void RegisterTodoRoutes(this WebApplication application)
    {
        var route = application.MapGroup(TodoPrefix).AddEndpointFilter<PerformanceFilter>();
        route.MapGet("/", GetAll.ExecuteAsync).AddEndpointFilter<GetAllTasksFilter>();
        route.MapGet("/{id}", SearchById.ExecuteAsync);
        route.MapPost("/", Create.ExecuteAsync).AddEndpointFilter<ValidateTodoFilter<AddTodoDto>>();
    }
}
