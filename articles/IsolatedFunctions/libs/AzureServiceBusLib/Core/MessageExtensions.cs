using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace AzureServiceBusLib.Core;

public static class MessageExtensions
{
    public static IMessageClientBuilder RegisterMessageClientBuilder(this IServiceCollection services)
    {
        var builder = new MessageClientBuilder(services);
        services.TryAddSingleton<IMessageClientBuilder, MessageClientBuilder>();
        return builder;
    }
}
