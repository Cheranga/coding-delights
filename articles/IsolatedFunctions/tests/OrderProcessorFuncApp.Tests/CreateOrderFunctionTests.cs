using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessorFuncApp.Features;

namespace OrderProcessorFuncApp.Tests;

public class CreateOrderFunctionTests
{
    private readonly JsonSerializerOptions _defaultSerializerOptions;

    public CreateOrderFunctionTests()
    {
        _defaultSerializerOptions = new()
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };
    }

    [Fact]
    public async Task Test1()
    {
        var context = new Mock<FunctionContext>();
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var createOrderRequest = new AutoFaker<CreateOrderRequestDto>().Generate();

        var request = new TestHttpRequestData<CreateOrderRequestDto>(context.Object, createOrderRequest);
        var function = new CreateOrderFunction(
            new CreateOrderRequestDtoValidator(),
            _defaultSerializerOptions,
            Mock.Of<ILogger<CreateOrderFunction>>()
        );
        var response = await function.Run(request, context.Object);
        Assert.NotNull(response.HttpResponse);
        Assert.Equal(HttpStatusCode.Accepted, response.HttpResponse.StatusCode);
    }

    [Fact(DisplayName = "Invalid create order request")]
    public async Task Test2()
    {
        var context = new Mock<FunctionContext>();
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var createOrderRequest = new AutoFaker<CreateOrderRequestDto>().Generate() with { OrderId = string.Empty };

        var request = new TestHttpRequestData<CreateOrderRequestDto>(context.Object, createOrderRequest);
        var function = new CreateOrderFunction(
            new CreateOrderRequestDtoValidator(),
            _defaultSerializerOptions,
            Mock.Of<ILogger<CreateOrderFunction>>()
        );
        var response = await function.Run(request, context.Object);
        Assert.NotNull(response.HttpResponse);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpResponse.StatusCode);
    }
}
