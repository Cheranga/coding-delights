using System.Text.Json;
using Azure.Messaging.ServiceBus;
using Azure.Storage.Queues;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Images;
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
    private IFutureDockerImage _functionImage;

    public async Task InitializeAsync()
    {
        // Create a network for the containers to communicate
        _network = new NetworkBuilder().Build();
        await _network.CreateAsync();

        // provisioning Azure Service Bus container
        var serviceBusContainer = await GetServiceBusContainer(_network, "sb-emulator", Path.GetFullPath("Config.json"));

        // provisioning Azurite container for Azure Storage emulation
        var azuriteContainer = await GetAzuriteContainer(_network, networkAlias: "azurite", queueNames: "processing-queue");

        _isolatedFunc = await GetFunctionContainer(
            "src/OrderProcessorFuncApp",
            _network,
            (azuriteContainer, "azurite"),
            (serviceBusContainer, "sb-emulator")
        );
        await _isolatedFunc.StartAsync();

        var uri = new UriBuilder("http", _isolatedFunc.Hostname, _isolatedFunc.GetMappedPublicPort(80)).Uri;
        Client = new HttpClient { BaseAddress = uri };
    }

    private string GetServiceBusConnectionString(string? networkAlias = null)
    {
        var connectionString = _serviceBusContainer.GetConnectionString();
        if (string.IsNullOrWhiteSpace(networkAlias))
        {
            return connectionString;
        }

        var hostServiceBusPort = _serviceBusContainer.GetMappedPublicPort(ServiceBusBuilder.ServiceBusPort);
        var hostHttpPort = _serviceBusContainer.GetMappedPublicPort(ServiceBusBuilder.ServiceBusHttpPort);
        return connectionString
            .Replace(_serviceBusContainer.Hostname, networkAlias)
            .Replace(hostServiceBusPort.ToString(), ServiceBusBuilder.ServiceBusPort.ToString())
            .Replace(hostHttpPort.ToString(), ServiceBusBuilder.ServiceBusHttpPort.ToString());
    }

    private async Task<IContainer> GetFunctionContainer(
        string dockerfileDirectory,
        INetwork network,
        (AzuriteContainer Azurite, string NetworkAlias) azuriteData,
        (ServiceBusContainer Container, string NetworkAlias) serviceBusData
    )
    {
        // Creating the function image from the Dockerfile
        _functionImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), dockerfileDirectory)
            .WithCleanUp(true)
            .Build();

        await _functionImage.CreateAsync();

        var azuriteBlobPort = azuriteData.Azurite.GetMappedPublicPort(AzuriteBuilder.BlobPort);
        var azuriteQueuePort = azuriteData.Azurite.GetMappedPublicPort(AzuriteBuilder.QueuePort);
        var azuriteTablePort = azuriteData.Azurite.GetMappedPublicPort(AzuriteBuilder.TablePort);
        var dnsAzuriteConnectionString = azuriteData
            .Azurite.GetConnectionString()
            .Replace(azuriteData.Azurite.Hostname, azuriteData.NetworkAlias)
            .Replace(azuriteBlobPort.ToString(), AzuriteBuilder.BlobPort.ToString())
            .Replace(azuriteQueuePort.ToString(), AzuriteBuilder.QueuePort.ToString())
            .Replace(azuriteTablePort.ToString(), AzuriteBuilder.TablePort.ToString());

        var hostServiceBusPort = _serviceBusContainer.GetMappedPublicPort(ServiceBusBuilder.ServiceBusPort);
        var hostHttpPort = _serviceBusContainer.GetMappedPublicPort(ServiceBusBuilder.ServiceBusHttpPort);
        var dnsServiceBusConnectionString = serviceBusData
            .Container.GetConnectionString()
            .Replace(_serviceBusContainer.Hostname, serviceBusData.NetworkAlias)
            .Replace(hostServiceBusPort.ToString(), ServiceBusBuilder.ServiceBusPort.ToString())
            .Replace(hostHttpPort.ToString(), ServiceBusBuilder.ServiceBusHttpPort.ToString());

        var container = new ContainerBuilder()
            .WithImage(_functionImage)
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

    private static async Task<AzuriteContainer> GetAzuriteContainer(INetwork network, string networkAlias, params string[] queueNames)
    {
        var container = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(network)
            .WithNetworkAliases(networkAlias)
            .WithAutoRemove(true)
            .WithPortBinding(10000, true) // Blob service
            .WithPortBinding(10001, true) // Queue service
            .WithPortBinding(10002, true) // Table service
            .Build();

        await container.StartAsync();

        var qsClient = new QueueServiceClient(
            container.GetConnectionString(),
            new QueueClientOptions { MessageEncoding = QueueMessageEncoding.Base64 }
        );
        foreach (var queueName in queueNames)
        {
            await qsClient.GetQueueClient(queueName).CreateIfNotExistsAsync();
        }

        return container;
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
        var serviceBusConnectionString = _serviceBusContainer.GetConnectionString();
        var serviceBusClient = new ServiceBusClient(serviceBusConnectionString);
        var sender = serviceBusClient.CreateSender(publishTo);

        var serviceBusMessage = new ServiceBusMessage(BinaryData.FromObjectAsJson(message, serializerOptions));
        foreach (var additionalProperty in additionalProperties)
        {
            serviceBusMessage.ApplicationProperties.Add(additionalProperty.Key, additionalProperty.Value);
        }

        return sender.SendMessageAsync(serviceBusMessage, token);
    }

    public Task<(string StdOut, string StdError)> GetFunctionLogs() => _isolatedFunc.GetLogsAsync();

    public async Task<ServiceBusContainer> GetServiceBusContainer(INetwork network, string networkAlias, string serviceBusConfigFullPath)
    {
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
            .WithPortBinding(ServiceBusBuilder.ServiceBusPort, true) // AMQP port
            .WithPortBinding(ServiceBusBuilder.ServiceBusHttpPort, true) // HTTP port
            .WithBindMount(serviceBusConfigFullPath, "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQL_SERVER", "db")
            .WithEnvironment("MSSQL_SA_PASSWORD", "YourStr0ng@Passw0rd")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Emulator Service is Successfully Up"))
            .Build();

        await serviceBus.StartAsync();

        _serviceBusContainer = serviceBus;
        return serviceBus;
    }

    public async Task DisposeAsync()
    {
        await _isolatedFunc.StopAsync();
        await _isolatedFunc.DisposeAsync();
        await _functionImage.DeleteAsync();
    }
}
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
