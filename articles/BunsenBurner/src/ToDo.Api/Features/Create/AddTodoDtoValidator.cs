using FluentValidation;

namespace ToDo.Api.Features.Create;

public class AddTodoDtoValidator : AbstractValidator<AddTodoDto>
{
    public AddTodoDtoValidator()
    {
        ClassLevelCascadeMode = CascadeMode.Stop;
        RuleFor(x => x.Title).NotNull().WithMessage("Title is required").NotEmpty().WithMessage("Title cannot be empty");
        RuleFor(x => x.Description)
            .NotNull()
            .WithMessage("Description is required")
            .NotEmpty()
            .WithMessage("Title cannot be empty");
        RuleFor(x => x.DueDate).GreaterThanOrEqualTo(DateTimeOffset.Now);
    }
}
