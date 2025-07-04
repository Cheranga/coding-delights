using FluentValidation;
using Microsoft.Azure.Functions.Worker.Http;

namespace OrderProcessorFuncApp.Core.Http;

public interface IApiRequestReader<TDto, TDtoValidator>
    where TDto : class, IApiRequestDto<TDto, TDtoValidator>
    where TDtoValidator : class, IValidator<TDto>
{
    Task<OperationResponse<FailedResult, SuccessResult<TDto>>> ReadRequestAsync(HttpRequestData request, CancellationToken token);
}
