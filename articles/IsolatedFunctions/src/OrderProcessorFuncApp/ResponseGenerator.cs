using System.Net;
using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp;

internal static class ResponseGenerator
{
    public static async Task<HttpResponseData> CreateErrorResponse(
        this HttpRequestData request,
        string errorCode,
        string errorMessage,
        HttpStatusCode statusCode
    )
    {
        var response = request.CreateResponse(statusCode);
        response.Headers.Add("Content-Type", "application/json");
        var errorResponse = new { ErrorCode = errorCode, ErrorMessage = errorMessage };
        await response.WriteAsJsonAsync(errorResponse);
        return response;
    }
}
