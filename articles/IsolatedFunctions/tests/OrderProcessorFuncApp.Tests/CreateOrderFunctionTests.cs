using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using Moq;
using OrderProcessorFuncApp.Features;

namespace OrderProcessorFuncApp.Tests;

public class CreateOrderFunctionTests
{
    private JsonSerializerOptions _defaultSerializerOptions;

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

internal sealed class TestHttpRequestData<TRequestData> : HttpRequestData
    where TRequestData : class
{
    private readonly HttpResponseData _response;
    private readonly HttpRequestMessage _httpRequestMessage;

    public TestHttpRequestData(FunctionContext functionContext, TRequestData requestData)
        : base(functionContext)
    {
        _httpRequestMessage = new HttpRequestMessage() { Content = JsonContent.Create(requestData) };
        _response = new TestHttpResponseData(functionContext);
    }

    public override HttpResponseData CreateResponse() => _response;

    public override Stream Body => _httpRequestMessage.Content!.ReadAsStream();
    public override HttpHeadersCollection Headers { get; }
    public override IReadOnlyCollection<IHttpCookie> Cookies { get; }
    public override Uri Url { get; }
    public override IEnumerable<ClaimsIdentity> Identities { get; }
    public override string Method { get; }
}

internal sealed class TestHttpResponseData : HttpResponseData
{
    public TestHttpResponseData(FunctionContext functionContext)
        : base(functionContext) { }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; }
    public override Stream Body { get; set; }
    public override HttpCookies Cookies { get; }
}
