using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Testcontainers.Azurite;
using Testcontainers.MsSql;
using Testcontainers.ServiceBus;

namespace OrderProcessorFuncApp.Integration.Tests;

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
public sealed class IsolatedFunctionsTestFixture : IAsyncLifetime
{
    private INetwork _network;
    private AzuriteContainer _azurite;
    private IContainer _isolatedFunc;
    private ServiceBusContainer _serviceBusContainer;

    public async Task InitializeAsync()
    {
        // Create a network for the containers to communicate
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        // provisioning Azure Service Bus container
        var serviceBusData = await GetServiceBusContainer(_network);

        // provisioning Azurite container for Azure Storage emulation
        var azuriteContainerData = await GetAzuriteContainer(_network, "processing-queue");

        _isolatedFunc = await GetFunctionContainer(
            "test-isolated-func",
            "src/OrderProcessorFuncApp",
            _network,
            azuriteContainerData,
            serviceBusData
        );
        await _isolatedFunc.StartAsync();

        var uri = new UriBuilder("http", _isolatedFunc.Hostname, _isolatedFunc.GetMappedPublicPort(80)).Uri;
        Client = new HttpClient { BaseAddress = uri };
    }

    private string GetServiceBusConnectionString() => _serviceBusContainer.GetConnectionString();

    private static async Task ProvisionQueues(AzuriteContainer azurite, params string[] queueNames)
    {
        await azurite.StartAsync();

        var qsClient = new QueueServiceClient(
            azurite.GetConnectionString(),
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );
        foreach (var queueName in queueNames)
            await qsClient.GetQueueClient(queueName).CreateIfNotExistsAsync();
    }

    private async Task<IContainer> GetFunctionContainer(
        string imageName,
        string dockerfileDirectory,
        INetwork network,
        (AzuriteContainer Azurite, string NetworkAlias) azuriteData,
        (ServiceBusContainer Container, string NetworkAlias) serviceBusData
    )
    {
        // Creating the function image from the Dockerfile
        var functionImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), dockerfileDirectory)
            .WithName(imageName)
            .WithCleanUp(true)
            .Build();

        await functionImage.CreateAsync();

        var dnsAzuriteConnectionString = azuriteData
            .Azurite.GetConnectionString()
            .Replace(azuriteData.Azurite.Hostname, azuriteData.NetworkAlias);
        var dnsServiceBusConnectionString = serviceBusData
            .Container.GetConnectionString()
            .Replace(serviceBusData.Container.Hostname, serviceBusData.NetworkAlias);

        var container = new ContainerBuilder()
            .WithImage(functionImage)
            .WithNetwork(network)
            .WithAutoRemove(true)
            .WithEnvironment("AzureWebJobsStorage", dnsAzuriteConnectionString)
            .WithEnvironment("AzureWebJobsQueueConnection", dnsAzuriteConnectionString)
            .WithEnvironment("AzureWebJobsAsbConnection", dnsServiceBusConnectionString)
            .WithEnvironment("StorageConfig__Connection", dnsAzuriteConnectionString)
            .WithEnvironment("StorageConfig__ProcessingQueueName", "processing-queue")
            .WithEnvironment("ServiceBusConfig__ProcessingQueueName", "temp-orders")
            .WithPortBinding(80, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(80).UntilMessageIsLogged("Application started"))
            .DependsOn(azuriteData.Azurite)
            .Build();

        return container;
    }

    private static async Task<(AzuriteContainer Container, string NetworkAlias)> GetAzuriteContainer(
        INetwork network,
        params string[] queueNames
    )
    {
        const string networkAlias = "azurite";

        var container = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(network)
            .WithNetworkAliases(networkAlias)
            .WithAutoRemove(true)
            .WithPortBinding(10000) // Blob service
            .WithPortBinding(10001) // Queue service
            .WithPortBinding(10002) // Table service
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(10000).UntilPortIsAvailable(10001).UntilPortIsAvailable(10002))
            .Build();

        await container.StartAsync();

        var qsClient = new QueueServiceClient(
            container.GetConnectionString(),
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );
        foreach (var queueName in queueNames)
            await qsClient.GetQueueClient(queueName).CreateIfNotExistsAsync();

        return (container, networkAlias);
    }

    public HttpClient Client { get; private set; }

    public Task PublishServiceBusMessage<TMessage>(
        string publishTo,
        TMessage message,
        JsonSerializerOptions serializerOptions,
        CancellationToken token,
        params (string Key, object Value)[] additionalProperties
    )
    {
        var serviceBusConnectionString = GetServiceBusConnectionString();
        var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        // Create a sender for the "orders" queue
        var sender = serviceBusClient.CreateSender(publishTo);

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(message, serializerOptions));
        foreach (var additionalProperty in additionalProperties)
            serviceBusMessage.ApplicationProperties.Add(additionalProperty.Key, additionalProperty.Value);

        return sender.SendMessageAsync(serviceBusMessage, token);
    }

    public Task<(string StdOut, string StdError)> GetFunctionLogs() => _isolatedFunc.GetLogsAsync();

    public async Task<(ServiceBusContainer Container, string NetworkAlias)> GetServiceBusContainer(INetwork network)
    {
        const string networkAlias = "sb-emulator";
        // MSSQL container
        var sqlContainer = new MsSqlBuilder()
            .WithPassword("YourStr0ng@Passw0rd")
            .WithNetwork(network)
            .WithNetworkAliases("db")
            .WithAutoRemove(true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(MsSqlBuilder.MsSqlPort))
            .Build();
        await sqlContainer.StartAsync();

        var serviceBus = new ServiceBusBuilder()
            .WithMsSqlContainer(network, sqlContainer, "db", "YourStr0ng@Passw0rd")
            .WithNetworkAliases(networkAlias)
            .WithAutoRemove(true)
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

        _serviceBusContainer = serviceBus;
        return (serviceBus, networkAlias);
    }

    public async Task DisposeAsync()
    {
        await _isolatedFunc.StopAsync();
        await _isolatedFunc.DisposeAsync();
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
