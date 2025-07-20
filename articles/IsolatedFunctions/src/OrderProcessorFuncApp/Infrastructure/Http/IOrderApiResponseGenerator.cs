using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Core;

namespace OrderProcessorFuncApp.Infrastructure.Http;

public interface IOrderApiResponseGenerator
{
    async Task<HttpResponseData> GenerateOrderAcceptedResponseAsync<TResponseData>(
        HttpRequestData request,
        TResponseData responseData,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(HttpStatusCode.Accepted);
        await httpResponse.WriteAsJsonAsync(responseData, cancellationToken: token);
        return httpResponse;
    }

    async Task<HttpResponseData> GenerateErrorResponseAsync(
        HttpRequestData request,
        ErrorResponse errorResponse,
        HttpStatusCode statusCode,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(statusCode);
        await httpResponse.WriteAsJsonAsync(errorResponse, cancellationToken: token);
        return httpResponse;
    }

    Task<HttpResponseData> GenerateErrorResponseAsync(
        HttpRequestData request,
        string errorCode,
        string errorMessage,
        HttpStatusCode statusCode,
        CancellationToken token
    ) => GenerateErrorResponseAsync(request, ErrorResponse.New(errorCode, errorMessage), statusCode, token);

    Task<HttpResponseData> GenerateErrorResponseAsync(
        HttpRequestData request,
        FailedResult failedResult,
        HttpStatusCode statusCode,
        CancellationToken token
    ) => GenerateErrorResponseAsync(request, failedResult.Error.ErrorCode, failedResult.Error.ErrorMessage, statusCode, token);
}
