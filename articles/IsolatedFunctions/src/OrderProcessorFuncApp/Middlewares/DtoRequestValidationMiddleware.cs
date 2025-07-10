using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;

namespace OrderProcessorFuncApp.Middlewares;

internal sealed class DtoRequestValidationMiddleware<TDto> : IFunctionsWorkerMiddleware
    where TDto : class
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var validator = context.InstanceServices.GetRequiredService<IValidator<TDto>>();
        if (validator is null)
        {
            await next(context);
            return;
        }
        var req = await context.GetHttpRequestDataAsync();
        if (req is not null)
        {
            try
            {
                var dto = await req.ReadFromJsonAsync<TDto>(cancellationToken: context.CancellationToken);
                if (dto is null)
                {
                    var nullData = req.CreateResponse(HttpStatusCode.BadRequest);
                    await nullData.WriteAsJsonAsync(
                        ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest),
                        cancellationToken: context.CancellationToken
                    );
                    context.GetInvocationResult().Value = nullData;
                    return;
                }
                var result = await validator.ValidateAsync(dto, context.CancellationToken);

                if (!result.IsValid)
                {
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(
                        ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest, result),
                        cancellationToken: context.CancellationToken
                    );
                    context.GetInvocationResult().Value = bad;
                    return;
                }

                context.Items["Dto"] = dto;
            }
            catch
            {
                var exceptionResponse = req.CreateResponse(HttpStatusCode.BadRequest);
                await exceptionResponse.WriteAsJsonAsync(
                    ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest),
                    cancellationToken: context.CancellationToken
                );
                context.GetInvocationResult().Value = exceptionResponse;
                return;
            }
        }

        // 6. All good — continue to the function
        await next(context);
    }
}
