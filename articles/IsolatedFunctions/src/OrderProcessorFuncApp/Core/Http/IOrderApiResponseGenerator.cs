using System.Net;
using System.Net.Mime;
using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Net.Http.Headers;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Core.Http;

public interface IOrderApiResponseGenerator
{
    async Task<CreateOrderApiResponse> GenerateOrderAcceptedResponseAsync(
        HttpRequestData request,
        Guid orderId,
        JsonSerializerOptions serializerOptions,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(HttpStatusCode.Accepted);
        httpResponse.Headers.Add("Content-Type", MediaTypeNames.Application.Json);
        var responseData = new OrderAcceptedResponse(orderId);
        await JsonSerializer.SerializeAsync(httpResponse.Body, responseData, serializerOptions, token);

        return new CreateOrderApiResponse { HttpResponse = httpResponse };
    }

    async Task<CreateOrderApiResponse> GenerateErrorResponseAsync(
        HttpRequestData request,
        ErrorResponse errorResponse,
        HttpStatusCode statusCode,
        JsonSerializerOptions serializerOptions,
        CancellationToken token
    )
    {
        var httpResponse = request.CreateResponse(statusCode);
        httpResponse.Headers.Add(HeaderNames.ContentType, MediaTypeNames.Application.Json);
        await JsonSerializer.SerializeAsync(httpResponse.Body, errorResponse, serializerOptions, token);

        return new CreateOrderApiResponse { HttpResponse = httpResponse };
    }
}
