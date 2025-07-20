using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using OrderProcessorFuncApp.Features.ProcessOrder;
using Testcontainers.Azurite;

namespace OrderProcessorFuncApp.Integration.Tests;

public class AnotherTryTest
{
    [Fact]
    public async Task Test1()
    {
        var customerCreatedEvent = new AutoFaker<CustomerCreatedEvent>().Generate();

        var functionImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "src/OrderProcessorFuncApp")
            .WithName(nameof(AzureFunctionsFixture).ToLowerInvariant())
            .Build();

        await functionImage.CreateAsync();

        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        var azurite = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(network)
            .WithNetworkAliases("azurite")
            .WithPortBinding(10000, 10000)
            .WithPortBinding(10001, 10001)
            .WithPortBinding(10002, 10002)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000).UntilPortIsAvailable(10001).UntilPortIsAvailable(10002))
            .Build();

        var originalAzuriteConnectionString =
            $"DefaultEndpointsProtocol=http;AccountName={AzuriteBuilder.AccountName};AccountKey={AzuriteBuilder.AccountKey};BlobEndpoint=http://127.0.0.1:10000/{AzuriteBuilder.AccountName};QueueEndpoint=http://127.0.0.1:10001/{AzuriteBuilder.AccountName};TableEndpoint=http://127.0.0.1:10002/{AzuriteBuilder.AccountName};";
        var dnsAzuriteOriginalConnectionString = originalAzuriteConnectionString.Replace("127.0.0.1", "azurite");
        var function = new ContainerBuilder()
            .WithImage(functionImage)
            .WithNetwork(network)
            // inside this network, “azurite” → the Azurite container
            .WithEnvironment("AzureWebJobsStorage", dnsAzuriteOriginalConnectionString)
            .WithEnvironment("StorageConfig__ProcessingQueueName", "processing-queue")
            .WithEnvironment("StorageConfig__ConnectionString", dnsAzuriteOriginalConnectionString)
            .WithPortBinding(80, 7071) // if you still want to hit it from your host on 7071
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .DependsOn(azurite)
            .Build();

        // Start the Azurite container first
        await azurite.StartAsync();

        // Create the queue and enqueue a message
        var qc = new QueueClient(
            originalAzuriteConnectionString,
            "processing-queue",
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );
        await qc.CreateIfNotExistsAsync();

        // Start the function container
        await function.StartAsync();

        // Enqueue the message
        var operation = await qc.SendMessageAsync(
            BinaryData.FromObjectAsJson(
                customerCreatedEvent,
                new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = false,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                }
            )
        );

        // Wait for the function to process the message
        await Task.Delay(TimeSpan.FromSeconds(5));

        // Check the logs to see if the message was processed
        // Later assert for middlewares, etc.
        var dataTuple = await function.GetLogsAsync();
        Assert.Contains("Processing order message:", dataTuple.Stdout, StringComparison.OrdinalIgnoreCase);
    }
}
