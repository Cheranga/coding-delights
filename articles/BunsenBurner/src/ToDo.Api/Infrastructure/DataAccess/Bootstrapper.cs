using Microsoft.EntityFrameworkCore;
using ToDo.Api.Features.Create;
using ToDo.Api.Features.GetAll;
using ToDo.Api.Features.SearchById;

namespace ToDo.Api.Infrastructure.DataAccess;

public static class Bootstrapper
{
    public static void RegisterDataAccessDependencies(IServiceCollection services, IConfiguration configuration)
    {
        services.AddScoped<ICommandHandler<CreateToDoCommand>, CreateToDoCommand.Handler>();
        services.AddScoped<IQueryHandler<SearchByIdQuery, TodoDataModel>, SearchByIdQuery.Handler>();
        services.AddScoped<IQueryHandler<SearchAllQuery, List<TodoDataModel>>, SearchAllQuery.Handler>();
        services.AddDbContext<TodoDbContext>(opt =>
        {
            opt.LogTo(Console.WriteLine);
            opt.UseInMemoryDatabase("TodoDb");
        });
    }
}
