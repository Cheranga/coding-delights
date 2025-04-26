using Microsoft.EntityFrameworkCore;

namespace ToDo.Api.Infrastructure.DataAccess;

// This must be a struct as EF works only with reference types

public class TodoDbContext(DbContextOptions<TodoDbContext> options) : DbContext(options)
{
    public DbSet<TodoDataModel> Todos => Set<TodoDataModel>();
}
