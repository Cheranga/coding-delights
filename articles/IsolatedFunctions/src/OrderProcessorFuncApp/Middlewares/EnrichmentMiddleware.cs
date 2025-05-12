using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Serilog.Context;

namespace OrderProcessorFuncApp.Middlewares;

internal sealed class EnrichmentMiddleware : IFunctionsWorkerMiddleware
{
    internal const string FunctionName = nameof(FunctionName);
    internal const string CorrelationId = nameof(CorrelationId);

    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;
        var correlationId = Guid.NewGuid().ToString("N");
        using (LogContext.PushProperty(FunctionName, functionName))
        using (LogContext.PushProperty(CorrelationId, correlationId))
        {
            await next(context);
        }
    }
}
