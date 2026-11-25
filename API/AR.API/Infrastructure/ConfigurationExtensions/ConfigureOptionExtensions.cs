using AR.Shared.Options;
using Microsoft.Extensions.Options;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureOptionExtensions
{
    internal static T GetOptionsExt<T>(this ConfigurationManager configurationManager, string name) =>
        configurationManager.GetSection(name).Get<T>() ?? throw new NullReferenceException(nameof(T));

    internal static IServiceCollection ConfigureOptionsExt(this IServiceCollection services,
                                                           ConfigurationManager configurationManager)
    {
        services.Configure<CorsSettings>(options => configurationManager
                                                   .GetSection(CorsSettings.Name)
                                                   .Bind(options));
        services.Configure<AppSettings>(options => configurationManager
                                                      .GetSection(AppSettings.Name)
                                                      .Bind(options));
        services.Configure<DataBaseSettings>(options => configurationManager
                                                         .GetSection(DataBaseSettings.Name)
                                                         .Bind(options));
        services.Configure<JwtSettings>(options => configurationManager
                                                   .GetSection(JwtSettings.Name)
                                                   .Bind(options));

        return services.ConfigureOptionsAsDependency();
    }
    
    private static IServiceCollection ConfigureOptionsAsDependency(this IServiceCollection services)
    {
        static string ExceptionMessageText(string text) => $"Can't resolve '{text}' configurations.";

        services.AddSingleton<CorsSettings>(serviceProvider =>
        {
            var options = serviceProvider.GetService<IOptions<CorsSettings>>();
            return options?.Value 
                   ?? throw new ApplicationException(ExceptionMessageText(nameof(CorsSettings)));
        });
        
        services.AddSingleton<AppSettings>(serviceProvider =>
        {
            var options = serviceProvider.GetService<IOptions<AppSettings>>();
            return options?.Value 
                   ?? throw new ApplicationException(ExceptionMessageText(nameof(AppSettings)));
        });

        services.AddSingleton<DataBaseSettings>(serviceProvider =>
        {
            var options = serviceProvider.GetService<IOptions<DataBaseSettings>>();
            return options?.Value 
                   ?? throw new ApplicationException(ExceptionMessageText(nameof(DataBaseSettings)));
        });

        services.AddSingleton<JwtSettings>(serviceProvider =>
        {
            var options = serviceProvider.GetService<IOptions<JwtSettings>>();
            return options?.Value 
                   ?? throw new ApplicationException(ExceptionMessageText(nameof(JwtSettings)));
        });

        return services;
    }
}