using AR.Core.EventHandlers;
using AR.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Core.Configurations;

internal static class EventHandlersDIModule
{
    private const string EventHandlerPostfix = "EventHandler";

    public static void Load(IServiceCollection services) =>
        services.AddServicesFromAssemblyExt(typeof(IEventHandler).Assembly, EventHandlerPostfix);
}