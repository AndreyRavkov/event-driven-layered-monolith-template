using AR.Core.EventQueueConsumers.Users;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Core.EventQueueConsumers.DI;

public static class QueueConsumerDIModule
{
    public static void Load(IServiceCollection services) =>
        // resolve as singleton to allow usage in BackgroundServices
        services.AddSingleton<IQueueConsumer, UserLoginEventQueueConsumer>();
}