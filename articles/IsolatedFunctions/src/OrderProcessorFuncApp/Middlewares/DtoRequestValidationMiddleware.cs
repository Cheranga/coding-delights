using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp.Middlewares;

internal sealed class DtoRequestValidationMiddleware<TDto>(ILogger<DtoRequestValidationMiddleware<TDto>> logger)
    : IFunctionsWorkerMiddleware
    where TDto : class
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var validator = context.InstanceServices.GetService<IValidator<TDto>>();
        if (validator is null)
        {
            logger.LogWarning("No validator found for {DtoType}. Skipping validation", typeof(TDto).Name);
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
                    logger.LogError("Received null DTO in request for {DtoType}", typeof(TDto).Name);
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
                    logger.LogError("Validation failed for {DtoType}: {Errors}", typeof(TDto).Name, result.Errors);
                    var bad = req.CreateResponse(HttpStatusCode.BadRequest);
                    await bad.WriteAsJsonAsync(
                        ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest, result),
                        cancellationToken: context.CancellationToken
                    );
                    context.GetInvocationResult().Value = bad;
                    return;
                }

                logger.LogInformation("Validation succeeded for {DtoType}", typeof(TDto).Name);
                context.Items["Dto"] = dto;
            }
            catch (Exception exception)
            {
                logger.LogError(exception, "Invalid data received for {DtoType}", typeof(TDto).Name);
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
