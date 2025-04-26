using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;
using ToDo.Api.Features.Create;
using ToDo.Api.Features.SearchById;
using static BunsenBurner.Bdd;

namespace ToDoApi.BunsenBurner.Tests.CreateTodo;

public class ValidateTodoFilterTests(WebApplicationFactory<Program> factory) : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact(DisplayName = "Given invalid task, when creating then must return bad response")]
    public async Task InvalidTask()
    {
        await Given(() =>
            {
                var dto = new Fixture().Build<AddTodoDto>().Without(x => x.Title).Create();
                return dto;
            })
            .When(async dto =>
            {
                var httpResponse = await _client.PostAsJsonAsync("/todos", dto);
                return httpResponse;
            })
            .Then(
                (_, response) =>
                {
                    response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
                }
            )
            .And(async response =>
            {
                var problemResponse = await JsonSerializer.DeserializeAsync<HttpValidationProblemDetails>(
                    await response.Content.ReadAsStreamAsync()
                );
                problemResponse.Should().NotBeNull();
                problemResponse!.Errors.Should().NotBeNullOrEmpty();
                problemResponse.Errors.Should().ContainKey("Title");
                problemResponse.Errors["Title"].Should().ContainSingle(x => string.Equals(x, "Title cannot be empty"));
            });
    }

    [Fact(DisplayName = "Given valid task, when creating then must return created response")]
    public async Task ValidTask()
    {
        await Given(() =>
            {
                var dto = new Fixture().Build<AddTodoDto>().With(x => x.DueDate, DateTimeOffset.Now.AddDays(1)).Create();
                return dto;
            })
            .When(async dto =>
            {
                var httpResponse = await _client.PostAsJsonAsync("/todos", dto);
                return httpResponse;
            })
            .Then(
                (_, response) =>
                {
                    response.StatusCode.Should().Be(HttpStatusCode.Created);
                }
            )
            .And(async response =>
            {
                response.Headers.TryGetValues("Location", out var location).Should().BeTrue();
                location.Should().NotBeNullOrEmpty();

                var todoResponse = await JsonSerializer.DeserializeAsync<TodoResponse>(
                    await response.Content.ReadAsStreamAsync(),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }
                );
                todoResponse.Should().NotBeNull();
                todoResponse!.Id.Should().NotBeNullOrEmpty();
            });
    }
}
