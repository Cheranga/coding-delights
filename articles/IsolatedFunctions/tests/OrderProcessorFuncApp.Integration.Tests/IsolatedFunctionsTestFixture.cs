using System.Text.Json;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using DotNet.Testcontainers.Networks;
using OrderProcessorFuncApp.Features.ProcessOrder;
using Testcontainers.Azurite;

namespace OrderProcessorFuncApp.Integration.Tests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public sealed class IsolatedFunctionsTestFixture : IAsyncLifetime
{
    private INetwork _network;
    private IContainer _azurite;
    private IContainer _isolatedFunc;
    private string _originalAzuriteConnectionString;
    private string _dnsAzuriteOriginalConnectionString;

    public async Task InitializeAsync()
    {
        // Create a network for the containers to communicate
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        // Create an Azurite container
        _azurite = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(_network)
            .WithNetworkAliases("azurite")
            .WithPortBinding(10000)
            .WithPortBinding(10001)
            .WithPortBinding(10002)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000).UntilPortIsAvailable(10001).UntilPortIsAvailable(10002))
            .Build();

        // Start the Azurite container first
        await _azurite.StartAsync();

        _originalAzuriteConnectionString =
            $"DefaultEndpointsProtocol=http;AccountName={AzuriteBuilder.AccountName};AccountKey={AzuriteBuilder.AccountKey};BlobEndpoint=http://127.0.0.1:{_azurite.GetMappedPublicPort(10000)}/{AzuriteBuilder.AccountName};QueueEndpoint=http://127.0.0.1:{_azurite.GetMappedPublicPort(10001)}/{AzuriteBuilder.AccountName};TableEndpoint=http://127.0.0.1:{_azurite.GetMappedPublicPort(10002)}/{AzuriteBuilder.AccountName};";

        var qc = new QueueClient(
            _originalAzuriteConnectionString,
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
        _dnsAzuriteOriginalConnectionString = _originalAzuriteConnectionString.Replace("127.0.0.1", "azurite");

        // Create the function container using the image and the Azurite connection string
        _isolatedFunc = new ContainerBuilder()
            .WithImage(functionImage)
            .WithNetwork(_network)
            // inside this network, “azurite” → the Azurite container
            .WithEnvironment("AzureWebJobsStorage", _dnsAzuriteOriginalConnectionString)
            .WithEnvironment("StorageConfig__ProcessingQueueName", "processing-queue")
            .WithEnvironment("StorageConfig__ConnectionString", _dnsAzuriteOriginalConnectionString)
            .WithPortBinding(80, true) // if you still want to hit it from your host on 7071
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80))
            .DependsOn(_azurite)
            .Build();

        // Start the function container
        await _isolatedFunc.StartAsync();

        var uri = new UriBuilder("http", _isolatedFunc.Hostname, _isolatedFunc.GetMappedPublicPort(80)).Uri;
        Client = new HttpClient() { BaseAddress = uri };
    }

    public HttpClient Client { get; private set; }

    public Task PublishCustomerCreatedEvent(CustomerCreatedEvent @event, JsonSerializerOptions serializerOptions)
    {
        var queueClient = new QueueClient(
            _originalAzuriteConnectionString,
            "processing-queue",
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );

        return queueClient.SendMessageAsync(BinaryData.FromObjectAsJson(@event, serializerOptions));
    }

    public Task<(string StdOut, string StdError)> GetFunctionLogs() => _isolatedFunc.GetLogsAsync();

    public async Task DisposeAsync()
    {
        await _isolatedFunc.StopAsync();
        await _isolatedFunc.DisposeAsync();

        await _azurite.StopAsync();
        await _azurite.DisposeAsync();

        await _network.DeleteAsync();
        await _network.DisposeAsync();
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
