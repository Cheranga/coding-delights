using System.Net;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using OrderProcessorFuncApp.Core.Http;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Tests;

public class CreateOrderFunctionTests
{
    private readonly OrderProcessor _orderProcessor;
    private readonly IApiRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator> _apiRequestReader;
    private readonly JsonSerializerOptions _serializerOptions;
    private readonly IOrderApiResponseGenerator _responseGenerator;

    public CreateOrderFunctionTests()
    {
        _serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        _apiRequestReader = new ApiRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator>(
            _serializerOptions,
            new CreateOrderRequestDto.Validator(new OrderItem.Validator()),
            NullLogger<ApiRequestReader<CreateOrderRequestDto, CreateOrderRequestDto.Validator>>.Instance
        );

        _responseGenerator = new OrderApiResponseGenerator();

        _orderProcessor = new OrderProcessor(NullLogger<OrderProcessor>.Instance);
    }

    [Fact(DisplayName = "Valid create order request returns accepted response")]
    public async Task Test1()
    {
        var context = new Mock<FunctionContext>();
        context.Setup(x => x.CancellationToken).Returns(CancellationToken.None);

        var createOrderRequest = new AutoFaker<CreateOrderRequestDto>().Generate();
        var request = new TestHttpRequestData<CreateOrderRequestDto>(context.Object, createOrderRequest);
        var function = new CreateOrderFunction(
            _apiRequestReader,
            _orderProcessor,
            _responseGenerator,
            _serializerOptions,
            NullLogger<CreateOrderFunction>.Instance
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
            _apiRequestReader,
            _orderProcessor,
            _responseGenerator,
            _serializerOptions,
            NullLogger<CreateOrderFunction>.Instance
        );
        var response = await function.Run(request, context.Object);
        Assert.NotNull(response.HttpResponse);
        Assert.Equal(HttpStatusCode.BadRequest, response.HttpResponse.StatusCode);
    }
}
