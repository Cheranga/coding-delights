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

    public HttpClient Client { get; private set; }

    public async Task InitializeAsync()
    {
        var azurite = new AzuriteBuilder().Build();
        await azurite.StartAsync();
        await _funcImage.CreateAsync();
        FuncContainer = new ContainerBuilder()
            .WithImage(_funcImage)
            .WithEnvironment("AzureWebJobsStorage", azurite.GetConnectionString())
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
