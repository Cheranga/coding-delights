using Testcontainers.ServiceBus;

namespace AzureServiceBusLib.Tests.Fixtures;

public class ServiceBusFixture : IAsyncLifetime
{
    public ServiceBusContainer Container { get; init; }

    public ServiceBusFixture()
    {
        Container = new ServiceBusBuilder()
            .WithAcceptLicenseAgreement(true)
            .WithResourceMapping("Config.json", "/ServiceBus_Emulator/ConfigFiles/")
            .Build();
    }

    public string GetConnectionString() => Container.GetConnectionString();

    public Task InitializeAsync() => Container.StartAsync();

    public async Task DisposeAsync()
    {
        await Container.StopAsync();
        await Container.DisposeAsync();
    }
}
