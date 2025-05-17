using Testcontainers.ServiceBus;

namespace OrderPublisher.Console.Tests;

public class ServiceBusFixture : IAsyncLifetime
{
    public ServiceBusFixture()
    {
        ServiceBusContainer = new ServiceBusBuilder()
            .WithAcceptLicenseAgreement(true)
            .WithResourceMapping("Config.json", "/ServiceBus_Emulator/ConfigFiles/")
            .Build();
    }

    public Task InitializeAsync() => ServiceBusContainer.StartAsync();

    public ServiceBusContainer ServiceBusContainer { get; init; }

    public Task DisposeAsync() => ServiceBusContainer.DisposeAsync().AsTask();
}
