using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessorFuncApp.Core.Http;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Tests;

public class CreateOrderFunctionTests
{
    private readonly OrderApiResponseGenerator _responseGenerator;
    private readonly OrderProcessor _orderProcessor;
    private readonly ITestHttpRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator> _mockedApiRequestReader;

    public CreateOrderFunctionTests()
    {
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        _mockedApiRequestReader = new TestHttpRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator>(
            serializerOptions,
            new CreateOrderRequestDto.Validator(),
            Mock.Of<ILogger<TestHttpRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator>>>()
        );

        _responseGenerator = new OrderApiResponseGenerator(serializerOptions);
        _orderProcessor = new OrderProcessor(Mock.Of<ILogger<OrderProcessor>>());
    }

    [Fact(DisplayName = "Valid create order request returns accepted response")]
    public async Task Test1()
    {
        var context = new Mock<FunctionContext>();
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var createOrderRequest = new AutoFaker<CreateOrderRequestDto>().Generate();
        var request = new TestHttpRequestData<CreateOrderRequestDto>(context.Object, createOrderRequest);
        var function = new CreateOrderFunction(
            _mockedApiRequestReader,
            _orderProcessor,
            _responseGenerator,
            Mock.Of<ILogger<CreateOrderFunction>>()
        );
        var response = await function.Run(request, context.Object);
        Assert.NotNull(response.HttpResponse);
        Assert.Equal(HttpStatusCode.Accepted, response.HttpResponse.StatusCode);
    }

    [Fact(DisplayName = "Invalid create order request returns bad request status code")]
    public async Task Test2()
    {
        var context = new Mock<FunctionContext>();
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var createOrderRequest = new AutoFaker<CreateOrderRequestDto>().Generate() with { OrderId = Guid.Empty };

        var request = new TestHttpRequestData<CreateOrderRequestDto>(context.Object, createOrderRequest);
        var function = new CreateOrderFunction(
            _mockedApiRequestReader,
            _orderProcessor,
            _responseGenerator,
            Mock.Of<ILogger<CreateOrderFunction>>()
        );
        var response = await function.Run(request, context.Object);
        Assert.NotNull(response.HttpResponse);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpResponse.StatusCode);
    }
}
