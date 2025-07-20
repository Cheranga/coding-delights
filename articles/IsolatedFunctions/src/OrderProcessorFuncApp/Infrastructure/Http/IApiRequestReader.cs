using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;
using OrderProcessorFuncApp.Core;

namespace OrderProcessorFuncApp.Infrastructure.Http;

public interface IApiRequestReader<TDto, TDtoValidator>
    where TDto : class, IApiRequestDto<TDto, TDtoValidator>
    where TDtoValidator : class, IValidator<TDto>
{
    Task<OperationResponse<FailedResult, SuccessResult<TDto>>> ReadRequestAsync(HttpRequestData request, CancellationToken token);
}
