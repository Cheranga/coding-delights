using System.Text.Json;
using Azure;
using Azure.Messaging.ServiceBus;
using Azure.Messaging.ServiceBus.Administration;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Extensions.Azure;
using OrderProcessorFuncApp.Domain.Messaging;
using Testcontainers.Azurite;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;
using Testcontainers.SqlEdge;

namespace OrderProcessorFuncApp.Integration.Tests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public sealed class IsolatedFunctionsTestFixture : IAsyncLifetime
{
    private INetwork _network;
    private AzuriteContainer _azurite;
    private IContainer _isolatedFunc;
    private IContainer _serviceBusContainer;

    public async Task InitializeAsync()
    {
        // Create a network for the containers to communicate
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        // provisioning servicebus container
        _serviceBusContainer = await GetServiceBusContainer(_network);
        var serviceBusConnectionString =
            @"Endpoint=sb://localhost;SharedAccessKeyName=RootManageSharedAccessKey;SharedAccessKey=SAS_KEY_VALUE;UseDevelopmentEmulator=true;";
        var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        // Create a sender for the "orders" queue
        var sender = serviceBusClient.CreateSender("temp-orders");
        await sender.SendMessageAsync(
            new ServiceBusMessage(BinaryData.FromString("test message")) { ApplicationProperties = { { "prop1", "1" } } }
        );

        _azurite = GetAzuriteContainer(_network);
        await ProvisionQueues(_azurite, "processing-queue");

        var dnsAsbConnectionString = serviceBusConnectionString.Replace("localhost", "sb-emulator");
        _isolatedFunc = await GetFunctionContainer(
            "test-isolated-func",
            "src/OrderProcessorFuncApp",
            _network,
            (_azurite, "azurite"),
            dnsAsbConnectionString
        );
        await _isolatedFunc.StartAsync();

        var uri = new UriBuilder("http", _isolatedFunc.Hostname, _isolatedFunc.GetMappedPublicPort(80)).Uri;
        Client = new HttpClient() { BaseAddress = uri };
    }

    private static async Task ProvisionQueues(AzuriteContainer azurite, params string[] queueNames)
    {
        await azurite.StartAsync();

        var qsClient = new QueueServiceClient(
            azurite.GetConnectionString(),
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );
        foreach (var queueName in queueNames)
        {
            await qsClient.GetQueueClient(queueName).CreateIfNotExistsAsync();
        }
    }

    private async Task<IContainer> GetFunctionContainer(
        string imageName,
        string dockerfileDirectory,
        INetwork network,
        (AzuriteContainer Azurite, string NetworkAlias) azuriteData,
        string dnsAsbConnectionString
    )
    {
        // Creating the function image from the Dockerfile
        var functionImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), dockerfileDirectory)
            .WithName(imageName)
            .WithCleanUp(true)
            .Build();

        await functionImage.CreateAsync();

        var dnsConnectionString = azuriteData.Azurite.GetConnectionString().Replace(_azurite.Hostname, azuriteData.NetworkAlias);
        var container = new ContainerBuilder()
            .WithImage(functionImage)
            .WithNetwork(network)
            // inside this network, “azurite” → the Azurite container
            .WithEnvironment("AzureWebJobsStorage", dnsConnectionString)
            .WithEnvironment("AzureWebJobsQueueConnection", dnsConnectionString)
            .WithEnvironment("AzureWebJobsAsbConnection", dnsAsbConnectionString)
            .WithEnvironment("StorageConfig__Connection", dnsConnectionString)
            .WithEnvironment("StorageConfig__ProcessingQueueName", "processing-queue")
            .WithPortBinding(80, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80).UntilMessageIsLogged("Application started"))
            .DependsOn(_azurite)
            .Build();

        return container;
    }

    private static AzuriteContainer GetAzuriteContainer(INetwork network)
    {
        var container = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(network)
            .WithNetworkAliases("azurite")
            .WithPortBinding(10000) // Blob service
            .WithPortBinding(10001) // Queue service
            .WithPortBinding(10002) // Table service
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000).UntilPortIsAvailable(10001).UntilPortIsAvailable(10002))
            .Build();

        return container;
    }

    public HttpClient Client { get; private set; }

    public Task PublishProcessOrderMessage(ProcessOrderMessage message, JsonSerializerOptions serializerOptions)
    {
        var queueClient = new QueueClient(
            _azurite.GetConnectionString(),
            "processing-queue",
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );

        return queueClient.SendMessageAsync(BinaryData.FromObjectAsJson(message, serializerOptions));
    }

    public Task<(string StdOut, string StdError)> GetFunctionLogs() => _isolatedFunc.GetLogsAsync();

    public async Task<IContainer> GetServiceBusContainer(INetwork network)
    {
        // MSSQL container
        var sqlContainer = new MsSqlBuilder()
            .WithPassword("YourStr0ng@Passw0rd")
            .WithNetwork(network)
            .WithNetworkAliases("db")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(1433))
            .Build();
        await sqlContainer.StartAsync();

        // Service Bus container
        var serviceBus = new ContainerBuilder()
            .WithImage("mcr.microsoft.com/azure-messaging/servicebus-emulator:latest")
            .WithNetwork(network)
            .WithNetworkAliases("sb-emulator")
            .WithPortBinding(ServiceBusBuilder.ServiceBusPort) // AMQP port
            .WithPortBinding(ServiceBusBuilder.ServiceBusHttpPort) // HTTP port
            .WithBindMount(Path.GetFullPath("Config.json"), "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQL_SERVER", "db")
            .WithEnvironment("MSSQL_SA_PASSWORD", "YourStr0ng@Passw0rd")
            .DependsOn(sqlContainer)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Emulator Service is Successfully Up"))
            .Build();

        await serviceBus.StartAsync();
        return serviceBus;
    }

    public async Task DisposeAsync()
    {
        await _serviceBusContainer.StopAsync();
        await _serviceBusContainer.DisposeAsync();

        await _isolatedFunc.StopAsync();
        await _isolatedFunc.DisposeAsync();

        await _azurite.StopAsync();
        await _azurite.DisposeAsync();

        await _network.DeleteAsync();
        await _network.DisposeAsync();
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
