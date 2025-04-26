namespace ToDo.Api.Infrastructure.DataAccess;

public interface IQueryHandler<in TQuery, TResponse>
    where TQuery : IQuery
    where TResponse : class
{
    ValueTask<TResponse?> QueryAsync(TQuery query, CancellationToken token);
}
