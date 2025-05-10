using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Core.Http;

public interface IHttpRequestReader
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<TRequest>>> ReadRequestAsync<TRequest>(
        HttpRequestData request,
        CancellationToken token
    )
        where TRequest : class;
}
