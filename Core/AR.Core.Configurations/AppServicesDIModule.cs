using AR.Core.AppServices.Infrastructure;
using AR.Core.Persistence.Context;
using AR.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Core.Configurations;

public static class AppServicesDIModule
{
    private const string AppServicePostfix = "AppService";
    private const string DomainServicePostfix = "DomainService";

    public static void Load(IServiceCollection services, string mainDb)
    {
        // load services
        services.AddServicesFromAssemblyExt(typeof(Try).Assembly, AppServicePostfix);
        services.AddServicesFromAssemblyExt(typeof(Try).Assembly, DomainServicePostfix);

        LoadCustomDiServices(services);

        DbContextConfiguration.LoadDbContext(services, mainDb);
        RepositoriesDIModule.Load(services);
        EventHandlersDIModule.Load(services);
    }

    private static void LoadCustomDiServices(IServiceCollection services) =>
        services.AddScoped<IEventDispatcher, EventDispatcher>();
}