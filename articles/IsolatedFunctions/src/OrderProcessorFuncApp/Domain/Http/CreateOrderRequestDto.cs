using FluentValidation;
using OrderProcessorFuncApp.Domain.Models;
using OrderProcessorFuncApp.Infrastructure.Http;

namespace OrderProcessorFuncApp.Domain.Http;

public sealed record CreateOrderRequestDto : IApiRequestDto<CreateOrderRequestDto, CreateOrderRequestDto.Validator>
{
    public Guid OrderId { get; set; }
    public Guid ReferenceId { get; set; }
    public DateTimeOffset OrderDate { get; set; }

    public IReadOnlyCollection<OrderItem> Items { get; set; } = [];

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
