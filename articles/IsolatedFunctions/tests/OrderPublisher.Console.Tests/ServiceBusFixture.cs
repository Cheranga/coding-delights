using Testcontainers.ServiceBus;

namespace OrderPublisher.Console.Tests;

public class ServiceBusFixture : IAsyncLifetime
{
    private readonly ServiceBusContainer _serviceBusContainer;

    public ServiceBusFixture()
    {
        _serviceBusContainer = new ServiceBusBuilder()
            .WithAcceptLicenseAgreement(true)
            .WithResourceMapping("Config.json", "/ServiceBus_Emulator/ConfigFiles/")
            .Build();
    }

    public Task InitializeAsync() => _serviceBusContainer.StartAsync();

    public Task DisposeAsync() => _serviceBusContainer.DisposeAsync().AsTask();

    public string GetConnectionString() => _serviceBusContainer.GetConnectionString();
}
