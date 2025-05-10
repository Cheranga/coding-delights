using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Core.Shared;
using OrderProcessorFuncApp.Features;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Core.Http;

public interface IOrderApiResponseGenerator
{
    Task<OrderAcceptedResponse> GenerateOrderAcceptedResponseAsync(HttpRequestData request, Guid orderId);

    Task<OrderAcceptedResponse> GenerateErrorResponseAsync(
        HttpRequestData request,
        OperationResult.FailedResult failure,
        HttpStatusCode statusCode,
        CancellationToken token
    );

    async Task<OrderAcceptedResponse> GenerateUnknownErrorAsync(HttpRequestData request, CancellationToken token)
    {
        var httpResponse = request.CreateResponse(HttpStatusCode.InternalServerError);
        httpResponse.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        await JsonSerializer.SerializeAsync(
            httpResponse.Body,
            ErrorResponse.New(ErrorCodes.Unknown, ErrorMessages.Unknown),
            cancellationToken: token
        );
        return new OrderAcceptedResponse { HttpResponse = httpResponse };
    }
}
