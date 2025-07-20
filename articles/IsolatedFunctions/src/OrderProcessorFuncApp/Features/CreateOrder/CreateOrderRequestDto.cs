using FluentValidation;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Infrastructure.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record CreateOrderRequestDto : IApiRequestDto<CreateOrderRequestDto, CreateOrderRequestDto.Validator>
{
    public Guid OrderId { get; init; }
    public Guid ReferenceId { get; init; }
    public DateTimeOffset OrderDate { get; init; }

    public IReadOnlyCollection<OrderItem> Items { get; init; } = [];

    public sealed class Validator : AbstractValidator<CreateOrderRequestDto>
    {
        public Validator(IValidator<OrderItem> orderItemValidator)
        {
            RuleFor(x => x.OrderId).NotEqual(Guid.Empty).WithMessage("OrderId is required");
            RuleFor(x => x.ReferenceId).NotEqual(Guid.Empty).WithMessage("ReferenceId is required");
            RuleFor(x => x.OrderDate).GreaterThan(DateTimeOffset.MinValue);
            RuleFor(x => x.Items).NotEmpty().WithMessage("Items are required");
            RuleForEach(x => x.Items).SetValidator(orderItemValidator);
        }
    }
}
