using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using Microsoft.Extensions.Logging;

namespace OrderProcessorFuncApp.Features;

public class TestFunction
{
    private readonly ILogger<TestFunction> _logger;

    public TestFunction(ILogger<TestFunction> logger)
    {
        _logger = logger;
    }

    [Function(nameof(TestFunction))]
    public async Task<TestResponse> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post")] HttpRequestData req)
    {
        _logger.LogInformation("Test function triggered");
        await Task.Delay(TimeSpan.FromSeconds(1));
        return new TestResponse("Test");
    }
}
