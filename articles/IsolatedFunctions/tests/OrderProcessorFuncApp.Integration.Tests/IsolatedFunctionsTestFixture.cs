using System.Security.Cryptography;
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

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

namespace OrderProcessorFuncApp.Integration.Tests;

public sealed class IsolatedFunctionsTestFixture : IAsyncLifetime
{
    private const string AzuriteAlias = "azurite";
    private const string ServiceBusAlias = "sb-emulator";
    private const string SqlServerAlias = "db";
    private IContainer _isolatedFunc;
    private IFutureDockerImage _functionImage;
    private ServiceBusContainer _serviceBusContainer;

    public async Task InitializeAsync()
    {
        // Create a network for the containers to communicate
        var network = new NetworkBuilder().Build();
        await network.CreateAsync();

        // provisioning MSSQL container
        var (mssqlContainer, dbPassword) = GetMSSqlContainer(network);

        // provisioning Azure Service Bus container
        _serviceBusContainer = SetupServiceBusContainer(network, mssqlContainer, dbPassword, Path.GetFullPath("Config.json"));

        // provisioning Azurite container for Azure Storage emulation
        var azuriteContainer = await SetupAzuriteContainer(network, queueNames: "processing-queue");
        await mssqlContainer.StartAsync();
        await _serviceBusContainer.StartAsync();

        // provisioning the isolated function container
        _isolatedFunc = await SetupFunctionContainer("src/OrderProcessorFuncApp", network, azuriteContainer, _serviceBusContainer);
        await _isolatedFunc.StartAsync();

        var uri = new UriBuilder("http", _isolatedFunc.Hostname, _isolatedFunc.GetMappedPublicPort(80)).Uri;
        Client = new HttpClient { BaseAddress = uri };
    }

    private async Task<IContainer> SetupFunctionContainer(
        string dockerfileDirectory,
        INetwork network,
        AzuriteContainer azuriteContainer,
        ServiceBusContainer serviceBusContainer
    )
    {
        // Creating the function image from the Dockerfile
        _functionImage = new ImageFromDockerfileBuilder()
            .WithDockerfileDirectory(CommonDirectoryPath.GetSolutionDirectory(), dockerfileDirectory)
            .WithCleanUp(true)
            .Build();

        await _functionImage.CreateAsync();

        var azuriteBlobPort = azuriteContainer.GetMappedPublicPort(AzuriteBuilder.BlobPort);
        var azuriteQueuePort = azuriteContainer.GetMappedPublicPort(AzuriteBuilder.QueuePort);
        var azuriteTablePort = azuriteContainer.GetMappedPublicPort(AzuriteBuilder.TablePort);
        var dnsAzuriteConnectionString = azuriteContainer
            .GetConnectionString()
            .Replace(azuriteContainer.Hostname, AzuriteAlias)
            .Replace(azuriteBlobPort.ToString(), AzuriteBuilder.BlobPort.ToString())
            .Replace(azuriteQueuePort.ToString(), AzuriteBuilder.QueuePort.ToString())
            .Replace(azuriteTablePort.ToString(), AzuriteBuilder.TablePort.ToString());

        var hostServiceBusPort = serviceBusContainer.GetMappedPublicPort(ServiceBusBuilder.ServiceBusPort);
        var hostHttpPort = serviceBusContainer.GetMappedPublicPort(ServiceBusBuilder.ServiceBusHttpPort);
        var dnsServiceBusConnectionString = serviceBusContainer
            .GetConnectionString()
            .Replace(_serviceBusContainer.Hostname, ServiceBusAlias)
            .Replace(hostServiceBusPort.ToString(), ServiceBusBuilder.ServiceBusPort.ToString())
            .Replace(hostHttpPort.ToString(), ServiceBusBuilder.ServiceBusHttpPort.ToString());

        var container = new ContainerBuilder()
            .WithImage(_functionImage)
            .WithNetwork(network)
            .WithAutoRemove(true)
            .WithEnvironment("AzureWebJobsStorage", dnsAzuriteConnectionString)
            .WithEnvironment("AzureWebJobsQueueConnection", dnsAzuriteConnectionString)
            .WithEnvironment("AzureWebJobsAsbConnection", dnsServiceBusConnectionString)
            .WithEnvironment("AZURE_FUNCTIONS_ENVIRONMENT", "Development")
            .WithEnvironment("StorageConfig__ProcessingQueueName", "processing-queue")
            .WithEnvironment("ServiceBusConfig__ProcessingQueueName", "temp-orders")
            .WithPortBinding(80, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(80).UntilMessageIsLogged("Application started"))
            .Build();

        return container;
    }

    private static async Task<AzuriteContainer> SetupAzuriteContainer(INetwork network, params string[] queueNames)
    {
        var container = new AzuriteBuilder()
            .WithImage("mcr.microsoft.com/azure-storage/azurite")
            .WithNetwork(network)
            .WithNetworkAliases(AzuriteAlias)
            .WithAutoRemove(true)
            .WithPortBinding(10000, assignRandomHostPort: true) // Blob service
            .WithPortBinding(10001, assignRandomHostPort: true) // Queue service
            .WithPortBinding(10002, assignRandomHostPort: true) // Table service
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

    private static (MsSqlContainer Container, string Password) GetMSSqlContainer(INetwork network)
    {
        var dbPassword = $"pwd{RandomNumberGenerator.GetHexString(6)}-666";
        var sqlContainer = new MsSqlBuilder()
            .WithPassword(dbPassword)
            .WithNetwork(network)
            .WithNetworkAliases("db")
            .WithAutoRemove(true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilInternalTcpPortIsAvailable(MsSqlBuilder.MsSqlPort))
            .Build();
        return (sqlContainer, dbPassword);
    }

    private static ServiceBusContainer SetupServiceBusContainer(
        INetwork network,
        MsSqlContainer sqlContainer,
        string dbPassword,
        string serviceBusConfigFullPath
    )
    {
        var serviceBus = new ServiceBusBuilder()
            .WithMsSqlContainer(network, sqlContainer, SqlServerAlias, dbPassword)
            .WithNetworkAliases(ServiceBusAlias)
            .WithAutoRemove(true)
            .WithPortBinding(ServiceBusBuilder.ServiceBusPort, assignRandomHostPort: true) // AMQP port
            .WithPortBinding(ServiceBusBuilder.ServiceBusHttpPort, assignRandomHostPort: true) // HTTP port
            .WithBindMount(serviceBusConfigFullPath, "/ServiceBus_Emulator/ConfigFiles/Config.json")
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("SQL_SERVER", SqlServerAlias)
            .WithEnvironment("MSSQL_SA_PASSWORD", dbPassword)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged("Emulator Service is Successfully Up"))
            .Build();
        return serviceBus;
    }

    public async Task DisposeAsync()
    {
        await _isolatedFunc.DisposeAsync();
        await _functionImage.DeleteAsync();
    }
}
