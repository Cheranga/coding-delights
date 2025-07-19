using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace OrderProcessorFuncApp.Middlewares;

internal sealed class PerformanceMonitoringMiddleware(ILogger<PerformanceMonitoringMiddleware> logger) : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        logger.LogInformation("Starting performance monitoring for function: {FunctionName}", context.FunctionDefinition.Name);

        var startTime = DateTime.UtcNow;
        using (LogContext.PushProperty("StartTime", startTime))
        {
            await next(context);
        }

        var endTime = DateTime.UtcNow;
        var duration = endTime - startTime;
        logger.LogInformation(
            "Function {FunctionName} completed in {Duration} ms",
            context.FunctionDefinition.Name,
            duration.TotalMilliseconds
        );
        LogContext.PushProperty("Duration", duration);
    }
}
