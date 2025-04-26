using System.Diagnostics;

namespace ToDo.Api.Filters;

public class PerformanceFilter(ILogger<PerformanceFilter> logger) : IEndpointFilter
{
    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        logger.LogInformation(
            "Starting request {HttpVerb} {@Url}",
            context.HttpContext.Request.Method,
            context.HttpContext.Request.Path
        );
        var stopWatch = new Stopwatch();
        stopWatch.Start();
        var response = await next(context);
        stopWatch.Stop();
        logger.LogInformation("Time taken {TimeTaken}ms", stopWatch.ElapsedMilliseconds);
        return response;
    }
}
