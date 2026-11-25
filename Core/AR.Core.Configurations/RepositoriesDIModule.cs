using AR.Core.Persistence.Context;
using AR.Core.Persistence.Handlers;
using AR.Core.Persistence.Interceptors;
using AR.Core.Persistence.Repositories;
using AR.Shared.Extensions;
using Microsoft.Extensions.DependencyInjection;

namespace AR.Core.Configurations;

internal static class RepositoriesDIModule
{
    private const string RepositoryPostfix = "Repository";
    private const string QueryHandlerPostfix = "QueryHandler";
    private const string CommandHandlerPostfix = "CommandHandler";

    internal static void Load(IServiceCollection services)
    {
        LoadCustomDiServices(services);

        services.AddServicesFromAssemblyExt(typeof(IRepository<>).Assembly, RepositoryPostfix);
        services.AddServicesFromAssemblyExt(typeof(BaseHandler).Assembly, QueryHandlerPostfix);
        services.AddServicesFromAssemblyExt(typeof(BaseHandler).Assembly, CommandHandlerPostfix);
    }

    private static void LoadCustomDiServices(IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        services.AddSingleton<UpdateAuditableEntityInterceptor>();
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
    }
}