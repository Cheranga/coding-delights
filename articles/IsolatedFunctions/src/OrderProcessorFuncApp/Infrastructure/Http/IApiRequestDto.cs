using FluentValidation;

namespace OrderProcessorFuncApp.Infrastructure.Http;

public interface IApiRequestDto<TDto, TDtoValidator>
    where TDto : class, IApiRequestDto<TDto, TDtoValidator>
    where TDtoValidator : class, IValidator<TDto>;
