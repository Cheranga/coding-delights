using System.Globalization;
using Microsoft.EntityFrameworkCore;
using ToDo.Api.Infrastructure.DataAccess;

namespace ToDo.Api.Features.SearchById;

public record SearchByIdQuery(string Id) : IQuery
{
    internal record Handler(TodoDbContext Context) : IQueryHandler<SearchByIdQuery, TodoDataModel>
    {
        public async ValueTask<TodoDataModel?> QueryAsync(SearchByIdQuery query, CancellationToken token)
        {
            var task = await Context
                .Todos.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == query.Id.ToUpper(CultureInfo.InvariantCulture), token);
            return task;
        }
    }
}
