using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Core.Shared;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Core.Http;

internal sealed class OrderApiResponseGenerator(JsonSerializerOptions serializerOptions) : IOrderApiResponseGenerator
{
    public async Task<OrderAcceptedResponse> GenerateOrderAcceptedResponseAsync(HttpRequestData request, Guid orderId)
    {
        var httpResponse = request.CreateResponse(HttpStatusCode.Accepted);
        httpResponse.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        var responseData = new OrderAcceptedData(orderId);
        await JsonSerializer.SerializeAsync(httpResponse.Body, responseData, serializerOptions);
        return new OrderAcceptedResponse { HttpResponse = httpResponse };
    }

    public async Task<OrderAcceptedResponse> GenerateErrorResponseAsync(
        HttpRequestData request,
        OperationResult.FailedResult failure,
        HttpStatusCode statusCode,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(statusCode);
        httpResponse.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        await JsonSerializer.SerializeAsync(httpResponse.Body, failure.Error, serializerOptions, token);
        return new OrderAcceptedResponse { HttpResponse = httpResponse };
    }
}
