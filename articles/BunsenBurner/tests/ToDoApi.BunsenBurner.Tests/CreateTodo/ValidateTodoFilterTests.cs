using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoBogus;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using ToDo.Api.Features.Create;
using ToDo.Api.Features.SearchById;
using static BunsenBurner.GivenWhenThen;

namespace ToDoApi.BunsenBurner.Tests.CreateTodo;

public class ValidateTodoFilterTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact(DisplayName = "Given invalid task, when creating then must return bad response")]
    public async Task InvalidTask()
    {
        await Given(() =>
            {
                var dto = new AutoFaker<AddTodoDto>().Generate() with { Title = string.Empty };
                return dto;
            })
            .When(async dto =>
            {
                var httpResponse = await _client.PostAsJsonAsync("/todos", dto);
                return httpResponse;
            })
            .Then((_, response) => response.StatusCode == HttpStatusCode.BadRequest)
            .And(async response =>
            {
                var problemResponse = await JsonSerializer.DeserializeAsync<HttpValidationProblemDetails>(
                    await response.Content.ReadAsStreamAsync()
                );
                Assert.NotNull(problemResponse);
                Assert.NotEmpty(problemResponse.Errors);
                Assert.NotEmpty(problemResponse.Errors);
                Assert.Contains(problemResponse.Errors, x => string.Equals(x.Key, "Title", StringComparison.Ordinal));
                Assert.Single(problemResponse.Errors["Title"], x => string.Equals(x, "Title cannot be empty", StringComparison.Ordinal));
            });
    }

    [Fact(DisplayName = "Given valid task, when creating then must return created response")]
    public async Task ValidTask()
    {
        await Given(() =>
            {
                var dto = new AutoFaker<AddTodoDto>().Generate() with { DueDate = DateTimeOffset.Now.AddDays(1) };
                return dto;
            })
            .When(async dto =>
            {
                var httpResponse = await _client.PostAsJsonAsync("/todos", dto);
                return httpResponse;
            })
            .Then((_, response) => response.StatusCode == HttpStatusCode.Created)
            .And(async response =>
            {
                Assert.True(response.Headers.TryGetValues("Location", out var location));
                Assert.NotNull(location);

                var todoResponse = await JsonSerializer.DeserializeAsync<TodoResponse>(
                    await response.Content.ReadAsStreamAsync(),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                );
                Assert.NotNull(todoResponse);
                Assert.NotNull(todoResponse.Id);
            });
    }
}
