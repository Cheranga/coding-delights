using FluentValidation;

namespace OrderProcessorFuncApp.Core.Http;

public interface ITestDto<TDto, TDtoValidator>
    where TDto : class, ITestDto<TDto, TDtoValidator>
    where TDtoValidator : class, IValidator<TDto>;
