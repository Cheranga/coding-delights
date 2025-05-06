using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Tests;

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
    public override HttpHeadersCollection Headers => new(_httpRequestMessage.Headers);
    public override IReadOnlyCollection<IHttpCookie> Cookies => new List<IHttpCookie>();
    public override Uri Url => _httpRequestMessage.RequestUri!;
    public override IEnumerable<ClaimsIdentity> Identities => new List<ClaimsIdentity>();
    public override string Method => _httpRequestMessage.Method.ToString();
}
