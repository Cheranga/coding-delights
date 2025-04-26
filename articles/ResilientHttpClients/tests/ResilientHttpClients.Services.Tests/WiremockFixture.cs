using WireMock.Server;

namespace ResilientHttpClients.Services.Tests;

public class WiremockFixture : IDisposable
{
    public WireMockServer Server { get; }

    public WiremockFixture()
    {
        Server = WireMockServer.Start();
    }

    public void Dispose()
    {
        Server.Stop();
        Server.Dispose();
    }
}
