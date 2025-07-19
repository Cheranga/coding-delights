using AutoBogus;
using Azure.Storage.Queues;
using OrderProcessorFuncApp.Features.CreateOrder;
using Testcontainers.Azurite;

namespace OrderProcessorFuncApp.Integration.Tests;

public class ProcessOrderFunctionTests(AzureFunctionsFixture fixture) : IClassFixture<AzureFunctionsFixture>
{
    [Fact(DisplayName = "Process order function should log message")]
    public async Task ProcessOrder_ShouldLogMessage()
    {
        var order = new AutoFaker<CreateOrderRequestDto>().Generate();
        var azuriteConnectionString =
            "DefaultEndpointsProtocol=http;"
            + "AccountName=devstoreaccount1;"
            + $"AccountKey={AzuriteBuilder.AccountKey};"
            + "BlobEndpoint=http://azurite:10000/devstoreaccount1;"
            + "QueueEndpoint=http://azurite:10001/devstoreaccount1;"
            + "TableEndpoint=http://azurite:10002/devstoreaccount1";

        var queueClient = fixture.QServiceClient.GetQueueClient("processing-queue");
        var operation = await queueClient.SendMessageAsync(BinaryData.FromObjectAsJson(order));
        Assert.NotNull(operation.Value.MessageId);
    }
}
