using System.Text.Json;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Core.Http;

internal sealed class HttpRequestReader(JsonSerializerOptions serializerOptions, ILogger<HttpRequestReader> logger) : IHttpRequestReader
{
    public async Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<TRequest>>> ReadRequestAsync<TRequest>(
        HttpRequestData request,
        CancellationToken token
    )
        where TRequest : class
    {
        try
        {
            var dto = await JsonSerializer.DeserializeAsync<TRequest>(request.Body, options: serializerOptions, cancellationToken: token);
            if (dto is not null)
            {
                return OperationResult.SuccessResult<TRequest>.New(dto);
            }

            logger.LogError("Request body is null");
            return OperationResult.FailedResult.New(ErrorCodes.InvalidRequestSchema, ErrorMessages.InvalidRequestSchema);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error when reading request");
            return OperationResult.FailedResult.New(ErrorCodes.InvalidRequestSchema, ErrorMessages.InvalidRequestSchema);
        }
    }
}
