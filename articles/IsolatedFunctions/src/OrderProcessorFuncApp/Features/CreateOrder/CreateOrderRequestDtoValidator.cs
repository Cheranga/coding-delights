using FluentValidation;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed class CreateOrderRequestDtoValidator : AbstractValidator<CreateOrderRequestDto>
{
    public CreateOrderRequestDtoValidator()
    {
        RuleFor(x => x.OrderId).NotEqual(Guid.Empty).WithMessage("OrderId is required");
        RuleFor(x => x.ReferenceId).NotEqual(Guid.Empty).WithMessage("ReferenceId is required");
    }
}
