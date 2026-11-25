using AR.API.Infrastructure.AuthHandlers;
using AR.ApiServices;
using AR.Infrastructure.DI;
using AR.Infrastructure.Jwt.DI;
using AR.Shared.Options;
using Microsoft.AspNetCore.Authorization;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureDIModulesExtensions
{
    internal static IServiceCollection ConfigureDIModulesExt(this IServiceCollection services, ConfigurationManager configurationManager)
    {
        // auth policy handlers
        services.AddScoped<IAuthorizationHandler, AccessPolicyHandler>();

        var dataBaseSettings = configurationManager.GetOptionsExt<DataBaseSettings>(DataBaseSettings.Name);
        ApiServicesDIModule.Load(services, dataBaseSettings.MainDb);

        // resolve infrastructure
        InfrastructureDIConfiguration.Load(services);

        // resolve Jwt token provider
        services.AddJwtTokenProvider();

        return services;
    }
}
