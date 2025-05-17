using System.Net.Http.Json;
using System.Security.Claims;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Integration.Tests;

internal sealed class TestHttpRequestData<TRequestData> : HttpRequestData
    where TRequestData : class
{
    private readonly HttpRequestMessage _httpRequestMessage;

    public TestHttpRequestData(FunctionContext functionContext, TRequestData requestData)
        : base(functionContext)
    {
        _httpRequestMessage = new HttpRequestMessage() { Content = JsonContent.Create(requestData) };
    }

    public override HttpResponseData CreateResponse() => new TestHttpResponseData(FunctionContext);

    public override Stream Body => _httpRequestMessage.Content!.ReadAsStream();
    public override HttpHeadersCollection Headers => new(_httpRequestMessage.Headers);
    public override IReadOnlyCollection<IHttpCookie> Cookies => new List<IHttpCookie>();
    public override Uri Url => _httpRequestMessage.RequestUri!;
    public override IEnumerable<ClaimsIdentity> Identities => new List<ClaimsIdentity>();
    public override string Method => _httpRequestMessage.Method.ToString();
}
