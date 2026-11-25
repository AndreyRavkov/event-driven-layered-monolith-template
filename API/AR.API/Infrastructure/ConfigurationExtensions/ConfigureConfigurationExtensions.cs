using AR.Infrastructure.Hosting;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureConfigurationExtensions
{
    internal static WebApplicationBuilder ConfigureConfigurationExt(this WebApplicationBuilder builder)
    {
        if (AREnvironment.IsLocal())
        {
            builder.Configuration.AddUserSecrets<Program>();
        }
        
        return builder;
    }
}