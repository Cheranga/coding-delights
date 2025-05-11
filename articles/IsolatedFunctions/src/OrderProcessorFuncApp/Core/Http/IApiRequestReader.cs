using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Core.Shared;

namespace OrderProcessorFuncApp.Core.Http;

public interface IApiRequestReader<TDto, TDtoValidator>
    where TDto : class, IApiRequestDto<TDto, TDtoValidator>
    where TDtoValidator : class, IValidator<TDto>
{
    Task<OperationResponse<OperationResult.FailedResult, OperationResult.SuccessResult<TDto>>> ReadRequestAsync(
        HttpRequestData request,
        CancellationToken token
    );
}
