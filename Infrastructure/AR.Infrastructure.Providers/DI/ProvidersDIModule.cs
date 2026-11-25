using Microsoft.Extensions.DependencyInjection;

namespace AR.Infrastructure.Providers.DI;

internal static class ProvidersDIModule
{
    internal static void Load(IServiceCollection services) =>
        services.AddScoped<IExecutionContextProvider, ExecutionContextProvider>();
}