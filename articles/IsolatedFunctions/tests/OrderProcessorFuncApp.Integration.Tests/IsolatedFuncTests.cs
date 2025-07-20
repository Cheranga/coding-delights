using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using OrderProcessorFuncApp.Features.ProcessOrder;

namespace OrderProcessorFuncApp.Integration.Tests;

[Collection(FunctionsTestFixtureCollection.Name)]
public class IsolatedFuncTests(IsolatedFunctionsTestFixture funcFixture)
{
    [Fact]
    public async Task Test1()
    {
        var customerCreatedEvent = new AutoFaker<CustomerCreatedEvent>().Generate();
        var serializerOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = false,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        };

        await funcFixture.PublishCustomerCreatedEvent(customerCreatedEvent, serializerOptions);
        await Task.Delay(TimeSpan.FromSeconds(5));
        var logData = await funcFixture.GetFunctionLogs();
        Assert.Contains("Processing order message:", logData.StdOut, StringComparison.OrdinalIgnoreCase);
    }
}
