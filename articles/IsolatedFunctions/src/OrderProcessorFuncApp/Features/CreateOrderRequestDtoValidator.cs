using FluentValidation;

namespace OrderProcessorFuncApp.Features;

public sealed class CreateOrderRequestDtoValidator : AbstractValidator<CreateOrderRequestDto>
{
    public CreateOrderRequestDtoValidator()
    {
        RuleFor(x => x.OrderId).NotEmpty().WithMessage("OrderId is required");
        RuleFor(x => x.ReferenceId).NotEmpty().WithMessage("ReferenceId is required");
    }
}
