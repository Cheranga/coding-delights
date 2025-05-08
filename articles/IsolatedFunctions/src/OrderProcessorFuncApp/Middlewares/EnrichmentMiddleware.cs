using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Middleware;
using Serilog.Context;

namespace OrderProcessorFuncApp.Middlewares;

internal sealed class EnrichmentMiddleware : IFunctionsWorkerMiddleware
{
    public async Task Invoke(FunctionContext context, FunctionExecutionDelegate next)
    {
        var functionName = context.FunctionDefinition.Name;
        var correlationId = Guid.NewGuid().ToString("N");
        using (LogContext.PushProperty("functionName", functionName))
        using (LogContext.PushProperty("correlationId", correlationId))
            await next(context);
    }
}
