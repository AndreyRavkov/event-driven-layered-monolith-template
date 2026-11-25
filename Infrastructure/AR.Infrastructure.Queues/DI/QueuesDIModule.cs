using Microsoft.Extensions.DependencyInjection;

namespace AR.Infrastructure.Queues.DI;

internal static class QueuesDIModule
{
    internal static void Load(IServiceCollection services)
    {
        services.AddSingleton<IChannelQueue, ChannelQueue>();

        services.AddScoped<IScopeQueue, ScopeQueue>();
    }
}