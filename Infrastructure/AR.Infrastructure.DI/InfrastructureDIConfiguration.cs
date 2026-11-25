using AR.Infrastructure.BackgroundServices.DI;
using AR.Infrastructure.Providers.DI;
using AR.Infrastructure.Queues.DI;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Infrastructure.DI;

public static class InfrastructureDIConfiguration
{
    public static void Load(IServiceCollection services)
    {
        BackgroundServicesDIModule.Load(services);
        QueuesDIModule.Load(services);
        ProvidersDIModule.Load(services);
    }
}