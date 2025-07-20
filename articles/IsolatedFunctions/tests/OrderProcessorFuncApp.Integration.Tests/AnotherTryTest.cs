using System.Text.Json;
using System.Text.Json.Serialization;
using AutoBogus;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using OrderProcessorFuncApp.Domain.Messaging;
using Testcontainers.Azurite;

namespace OrderProcessorFuncApp.Integration.Tests;

public class AnotherTryTest
{
    [Fact(Skip = "This test is no longer required. This was monument to design the collection fixture and the tests associated with it.")]
    public async Task Test1()
    {
        // Create a network for the containers to communicate
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // Create an Azurite container
        var azurite = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(network)
            .WithNetworkAliases("azurite")
            .WithPortBinding(10000)
            .WithPortBinding(10001)
            .WithPortBinding(10002)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000).UntilPortIsAvailable(10001).UntilPortIsAvailable(10002))
            .Build();

        // Start the Azurite container first
        await azurite.StartAsync();

        // Create the queue
        var originalAzuriteConnectionString =
            $"DefaultEndpointsProtocol=http;AccountName={AzuriteBuilder.AccountName};AccountKey={AzuriteBuilder.AccountKey};BlobEndpoint=http://127.0.0.1:{azurite.GetMappedPublicPort(10000)}/{AzuriteBuilder.AccountName};QueueEndpoint=http://127.0.0.1:{azurite.GetMappedPublicPort(10001)}/{AzuriteBuilder.AccountName};TableEndpoint=http://127.0.0.1:{azurite.GetMappedPublicPort(10002)}/{AzuriteBuilder.AccountName};";
        var qc = new QueueClient(
            originalAzuriteConnectionString,
            "processing-queue",
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );
        await qc.CreateIfNotExistsAsync();

        // Build the function image from the Dockerfile
        var functionImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "src/OrderProcessorFuncApp")
            .WithName("test-isolated-func")
            .WithCleanUp(true)
            .Build();

        await functionImage.CreateAsync();

        // Replace the localhost IP with the DNS name of the Azurite container
        var dnsAzuriteOriginalConnectionString = originalAzuriteConnectionString.Replace("127.0.0.1", "azurite");
        // Create the function container using the image and the Azurite connection string
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

        // Start the function container
        await function.StartAsync();

        // Enqueue customer-created event to the queue
        var processOrderMessage = new AutoFaker<ProcessOrderMessage>().Generate();
        await qc.SendMessageAsync(
            BinaryData.FromObjectAsJson(
                processOrderMessage,
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
