using System.Net;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Features;

namespace OrderProcessorFuncApp.Core;

internal static class ResponseGenerator
{
    public static async Task<OrderAcceptedResponse> CreateErrorResponse(
        this HttpRequestData request,
        string errorCode,
        string errorMessage,
        HttpStatusCode statusCode,
        JsonSerializerOptions serializerOptions,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(statusCode);
        httpResponse.Headers.Add("Content-Type", "application/json");
        var errorResponse = new { ErrorCode = errorCode, ErrorMessage = errorMessage };
        await JsonSerializer.SerializeAsync(httpResponse.Body, errorResponse, serializerOptions, token);
        return new OrderAcceptedResponse { HttpResponse = httpResponse };
    }

    public static async Task<OrderAcceptedResponse> CreateSuccessResponse(
        this HttpRequestData request,
        HttpStatusCode statusCode,
        OrderAcceptedData orderAcceptedData,
        JsonSerializerOptions serializerOptions,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(statusCode);
        httpResponse.Headers.Add("Content-Type", "application/json");
        await JsonSerializer.SerializeAsync(httpResponse.Body, orderAcceptedData, serializerOptions, token);
        return new OrderAcceptedResponse { HttpResponse = httpResponse };
    }
}
