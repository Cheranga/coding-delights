using System.Net;
using AutoFixture;
using FluentAssertions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.Extensions.Logging;
using Moq;
using ToDo.Api.Features.Create;
using ToDo.Api.Features.SearchById;
using ToDo.Api.Infrastructure.DataAccess;
using static BunsenBurner.Aaa;
using Operations = ToDo.Api.Features.Create.Operations;

namespace ToDoApi.BunsenBurner.Tests.CreateTodo;

public static class OperationsTests
{
    [Fact(DisplayName = "Error occurs when saving to database")]
    public static async Task ErrorOccursWhenSavingToDatabase() =>
        await Arrange(() =>
            {
                var commandHandler = new Mock<ICommandHandler<CreateToDoCommand>>();

                commandHandler
                    .Setup(x => x.ExecuteAsync(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
                    .Throws(new Exception("error!"));

                var logger = new Mock<ILogger<Program>>();

                var task = new Fixture().Create<AddTodoDto>();
                return (commandHandler, logger, task);
            })
            .Act(async data => await Operations.ExecuteAsync(data.commandHandler.Object, data.logger.Object, data.task))
            .Assert(
                (_, response) =>
                {
                    var problem = response.Result switch
                    {
                        ProblemHttpResult p => p.ProblemDetails,
                        _ => null,
                    };
                    problem!.Status.Should().Be((int)HttpStatusCode.InternalServerError);
                    problem.Detail.Should().Be("error occurred when adding a task");
                }
            );

    [Fact(DisplayName = "Valid task will be saved successfully and task id will be returned")]
    public static async Task ValidTask() =>
        await Arrange(() =>
            {
                var commandHandler = new Mock<ICommandHandler<CreateToDoCommand>>();
                commandHandler
                    .Setup(x => x.ExecuteAsync(It.IsAny<CreateToDoCommand>(), It.IsAny<CancellationToken>()))
                    .ReturnsAsync("666");

                var logger = new Mock<ILogger<Program>>();

                var task = new Fixture().Create<AddTodoDto>();
                return (commandHandler, logger, task);
            })
            .Act(async data =>
            {
                var response = await Operations.ExecuteAsync(data.commandHandler.Object, data.logger.Object, data.task);
                return response;
            })
            .Assert(
                (data, response) =>
                {
                    var model = response.Result switch
                    {
                        Created<TodoResponse> created => created.Value,
                        _ => null,
                    };
                    model.Should().NotBeNull();
                    model!.Id.Should().Be("666");
                    model.Title.Should().Be(data.task.Title);
                    model.Description.Should().Be(data.task.Description);
                    model.DueDate.Should().Be(data.task.DueDate);
                }
            );
}
