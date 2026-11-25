using AR.Shared.Options;
using Microsoft.Net.Http.Headers;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureCorsExtensions
{
    private const string CorsPolicyName = "CORS POLICY";
    
    internal static IServiceCollection ConfigureCorsExt(
        this IServiceCollection services,
        ConfigurationManager configurationManager)
    {
        var corsSettings = configurationManager.GetOptionsExt<CorsSettings>(CorsSettings.Name);

        services.AddCors(options =>
                             options.AddPolicy(CorsPolicyName,
                                               policy => policy
                                                         .WithOrigins(corsSettings.GetOrigins())
                                                         .WithMethods(corsSettings.GetMethods())
                                                         .SetPreflightMaxAge(TimeSpan.FromMinutes(5))
                                                         .AllowAnyHeader()
                                                         .AllowCredentials()
                                                         .WithExposedHeaders(HeaderNames.ContentDisposition)));
        
        return services;
    }
    
    internal static IApplicationBuilder UseCorsExt(this IApplicationBuilder app)
    {
        app.UseCors(CorsPolicyName);
        
        return app;
    }
}