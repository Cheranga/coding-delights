using System.Net;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Core.Http;

public interface IOrderApiResponseGenerator
{
    async Task<HttpResponseData> GenerateOrderAcceptedResponseAsync(HttpRequestData request, Guid orderId, CancellationToken token)
    {
        var httpResponse = request.CreateResponse(HttpStatusCode.Accepted);
        await httpResponse.WriteAsJsonAsync(new OrderAcceptedResponse(orderId), cancellationToken: token);
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
        // Serialize the error response to JSON and set it as the content of the response
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
