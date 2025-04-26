using FluentValidation;
using ToDo.Api;
using ToDo.Api.Infrastructure.DataAccess;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDistributedMemoryCache();
builder.Services.AddValidatorsFromAssemblyContaining<Program>();
Bootstrapper.RegisterDataAccessDependencies(builder.Services, builder.Configuration);

var app = builder.Build();

app.UseHttpsRedirection();

app.RegisterTodoRoutes();

await app.RunAsync();

#pragma warning disable S1118
public partial class Program;
#pragma warning restore S1118
