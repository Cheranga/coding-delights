using FluentValidation;
using OrderProcessorFuncApp.Infrastructure.Http;

namespace OrderProcessorFuncApp.Domain.Models;

public sealed record OrderItem : IApiRequestDto<OrderItem, OrderItem.Validator>
{
    public required string ProductId { get; set; }
    public required decimal Quantity { get; set; }
    public required decimal Price { get; set; }
    public required string Metric { get; set; }

    public sealed class Validator : AbstractValidator<OrderItem>
    {
        public Validator()
        {
            RuleFor(x => x.ProductId).NotEmpty().WithMessage("ProductId is required").NotNull().WithMessage("ProductId is required");
            RuleFor(x => x.Quantity).GreaterThan(0).WithMessage("Quantity must be greater than 0");
            RuleFor(x => x.Price).GreaterThan(0).WithMessage("Price must be greater than 0");
            RuleFor(x => x.Metric).NotEmpty().WithMessage("Metric is required").NotNull().WithMessage("Metric is required");
        }
    }
}
