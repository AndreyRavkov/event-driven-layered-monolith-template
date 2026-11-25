using AR.Core.Configurations;
using AR.Shared.Extensions;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AR.ApiServices;

public class ApiServicesDIModule
{
    private const string Postfix = "ApiService";

    public static void Load(IServiceCollection services, string mainDb)
    {
        // load validators
        services.AddValidatorsFromAssemblyContaining<ApiServicesDIModule>();

        // load services
        services.AddServicesFromAssemblyExt(typeof(ApiServicesDIModule).Assembly, Postfix);

        // load custom DI
        // LoadCustomDiServices(services);

        AppServicesDIModule.Load(services, mainDb);
    }

    // private static void LoadCustomDiServices(IServiceCollection services)
    // {
    //     // TBD
    // }
}