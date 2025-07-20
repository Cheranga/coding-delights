using Azure.Core;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
using Testcontainers.Azurite;

namespace OrderProcessorFuncApp.Integration.Tests;

public sealed class AzureFunctionsFixture : IAsyncLifetime
{
    private readonly IFutureDockerImage _funcImage = new ImageFromDockerfileBuilder()
        .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), "src/OrderProcessorFuncApp")
        .WithName(nameof(AzureFunctionsFixture).ToLowerInvariant())
        .Build();

    public IContainer FuncContainer { get; private set; }

    public AzuriteContainer Azurite { get; set; }

    public HttpClient Client { get; private set; }

    public QueueServiceClient QServiceClient { get; private set; }

    public async Task InitializeAsync()
    {
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        var azurite = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite:latest")
            .WithNetwork(network)
            .WithNetworkAliases("azurite")
            .Build();
        await azurite.StartAsync();
        Azurite = azurite;

        var azuriteConnectionString =
            "DefaultEndpointsProtocol=http;"
            + "AccountName=devstoreaccount1;"
            + $"AccountKey={AzuriteBuilder.AccountKey};"
            + "BlobEndpoint=http://azurite:10000/devstoreaccount1;"
            + "QueueEndpoint=http://azurite:10001/devstoreaccount1;"
            + "TableEndpoint=http://azurite:10002/devstoreaccount1";
        QServiceClient = new QueueServiceClient(
            azurite.GetConnectionString(),
            new QueueClientOptions
            {
                Retry =
                {
                    Mode = RetryMode.Exponential,
                    MaxRetries = 5,
                    Delay = TimeSpan.FromSeconds(2),
                    MaxDelay = TimeSpan.FromSeconds(10),
                },
            }
        );
        var queueClient = QServiceClient.GetQueueClient("processing-queue");
        await queueClient.CreateIfNotExistsAsync();
        await _funcImage.CreateAsync();
        FuncContainer = new ContainerBuilder()
            .WithImage(_funcImage)
            .WithNetwork(network)
            .WithNetworkAliases("azurite")
            .WithEnvironment("AzureWebJobsStorage", azurite.GetConnectionString())
            .WithEnvironment("StorageConfig__ProcessingQueueName", "processing-queue")
            .WithEnvironment("StorageConfig__ConnectionString", azurite.GetConnectionString())
            .WithPortBinding(80, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Application started"))
            .Build();

        await FuncContainer.StartAsync();

        var uri = new UriBuilder("http", FuncContainer.Hostname, FuncContainer.GetMappedPublicPort(80)).Uri;
        Client = new HttpClient() { BaseAddress = uri };
    }

    public async Task DisposeAsync()
    {
        await FuncContainer.StopAsync();
        await FuncContainer.DisposeAsync();
    }
}
