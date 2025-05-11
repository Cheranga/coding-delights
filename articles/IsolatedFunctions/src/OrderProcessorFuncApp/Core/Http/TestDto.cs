using FluentValidation;

namespace OrderProcessorFuncApp.Core.Http;

public sealed record TestDto : ITestDto<TestDto, TestDto.Validator>
{
    public string Name { get; init; } = string.Empty;
    public int Age { get; init; }

    public class Validator : AbstractValidator<TestDto>
    {
        public Validator()
        {
            RuleFor(x => x.Name).NotEmpty();
            RuleFor(x => x.Age).GreaterThan(0);
        }
    }
}
