using AR.Core.EventQueueConsumers.DI;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Infrastructure.BackgroundServices.DI;

public static class BackgroundServicesDIModule
{
    public static void Load(IServiceCollection services)
    {
        QueueConsumerDIModule.Load(services);

        services.AddHostedService<ChannelQueueBackgroundService>();
    }
}