using System.Diagnostics.CodeAnalysis;
using System.Net;
using FluentValidation;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OrderProcessorFuncApp.Core;
using OrderProcessorFuncApp.Domain;
using OrderProcessorFuncApp.Features.CreateOrder;

namespace OrderProcessorFuncApp.Middlewares;

[SuppressMessage("AsyncUsage", "AsyncFixer01:Unnecessary async/await usage")]
[SuppressMessage("Major Code Smell", "S125:Sections of code should not be commented out")]
internal sealed class DtoRequestValidationMiddleware<TDto>(ILogger<DtoRequestValidationMiddleware<TDto>> logger)
    : IFunctionsWorkerMiddleware
    where TDto : class
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        // var validator = context.InstanceServices.GetService<IValidator<TDto>>();
        // if (validator is null)
        // {
        //     logger.LogWarning("No validator found for {DtoType}. Skipping validation", typeof(TDto).Name);
        //     await next(context);
        //     return;
        // }
        //
        // var req = await context.GetHttpRequestDataAsync();
        // if (req is not null)
        // {
        //     try
        //     {
        //         var dto = await req.ReadFromJsonAsync<TDto>(cancellationToken: context.CancellationToken);
        //         if (dto is null)
        //         {
        //             logger.LogError("Received null DTO in request for {DtoType}", typeof(TDto).Name);
        //             var badDataResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        //             await badDataResponse.WriteAsJsonAsync(
        //                 ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest),
        //                 cancellationToken: context.CancellationToken
        //             );
        //             context.GetInvocationResult().Value = badDataResponse;
        //
        //             return;
        //         }
        //
        //         var result = await validator.ValidateAsync(dto, context.CancellationToken);
        //
        //         if (!result.IsValid)
        //         {
        //             logger.LogError("Validation failed for {DtoType}: {Errors}", typeof(TDto).Name, result.Errors);
        //             var bad = req.CreateResponse(HttpStatusCode.BadRequest);
        //             await bad.WriteAsJsonAsync(
        //                 ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest, result),
        //                 cancellationToken: context.CancellationToken
        //             );
        //
        //             context.GetInvocationResult().Value = new OrderAcceptedResponse { HttpResponse = bad, Message = null };
        //             return;
        //         }
        //
        //         logger.LogInformation("Validation succeeded for {DtoType}", typeof(TDto).Name);
        //         context.Items["Dto"] = dto;
        //     }
        //     catch (Exception exception)
        //     {
        //         logger.LogError(exception, "Invalid data received for {DtoType}", typeof(TDto).Name);
        //         var exceptionResponse = req.CreateResponse(HttpStatusCode.BadRequest);
        //         await exceptionResponse.WriteAsJsonAsync(
        //             ErrorResponse.New(ErrorCodes.InvalidDataInRequest, ErrorMessages.InvalidDataInRequest),
        //             cancellationToken: context.CancellationToken
        //         );
        //         context.GetInvocationResult().Value = exceptionResponse;
        //         return;
        //     }
        // }

        // 6. All good — continue to the function
        logger.LogInformation("Request validation middleware called");
        await next(context);
    }
}
