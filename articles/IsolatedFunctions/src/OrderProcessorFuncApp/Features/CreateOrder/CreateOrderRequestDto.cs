using FluentValidation;
using OrderProcessorFuncApp.Core.Http;

namespace OrderProcessorFuncApp.Features.CreateOrder;

public sealed record CreateOrderRequestDto : ITestDto<CreateOrderRequestDto, CreateOrderRequestDto.Validator>
{
    public required Guid OrderId { get; init; }
    public required Guid ReferenceId { get; init; }
    public required DateTimeOffset OrderDate { get; init; }

    public required IReadOnlyCollection<OrderItem> Items { get; init; }

    public sealed class Validator : AbstractValidator<CreateOrderRequestDto>
    {
        public Validator()
        {
            RuleFor(x => x.OrderId).NotEqual(Guid.Empty).WithMessage("OrderId is required");
            RuleFor(x => x.ReferenceId).NotEqual(Guid.Empty).WithMessage("ReferenceId is required");
        }
    }
}
