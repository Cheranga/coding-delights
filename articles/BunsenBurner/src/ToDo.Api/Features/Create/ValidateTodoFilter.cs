using FluentValidation;

namespace ToDo.Api.Features.Create;

public class ValidateTodoFilter<T>(IValidator<T> validator, ILogger<ValidateTodoFilter<T>> logger) : IEndpointFilter
    where T : class
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var model = context.GetArgument<T>(2);
        var validationResult = await validator.ValidateAsync(model);
        if (!validationResult.IsValid)
            return TypedResults.ValidationProblem(validationResult.ToDictionary());
        logger.LogInformation("Valid request");
        return await next(context);
    }
}
