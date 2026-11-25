using AR.Infrastructure.Hosting;
using Serilog;
using Serilog.Events;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureLoggingExtensions
{
    internal static WebApplicationBuilder ConfigureLoggingExt(this WebApplicationBuilder builder)
    {
        builder.Host.UseSerilog((ctx, loggerConfig) =>
            {
                ExpandHomeDirectoryInPaths(ctx.Configuration);

                var logEventLevel = AREnvironment.IsLocal()  ? LogEventLevel.Debug :  LogEventLevel.Warning;
                loggerConfig
                    .MinimumLevel.Override("Microsoft", logEventLevel)
                    .MinimumLevel.Override("Azure.Core", logEventLevel)
                    .MinimumLevel.Override("Azure.Storage.Blobs", logEventLevel)
                    .MinimumLevel.Override("System", logEventLevel)
                    .MinimumLevel.Override("Microsoft.AspNetCore.SignalR", logEventLevel)
                    .ReadFrom.Configuration(ctx.Configuration)
                    .Enrich.FromLogContext()
                    .Enrich.WithEnvironmentName()
                    .Enrich.WithThreadId()
                    .Enrich.WithThreadName();
            }
        );
        
        return builder;
    }

    // Serilog does not expand "~", so "~/logs/..." would create a folder literally named "~"
    // in the working directory. Replace a leading "~" with the user's home directory.
    private static void ExpandHomeDirectoryInPaths(IConfiguration configuration)
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        foreach (var (key, value) in configuration.GetSection("Serilog").AsEnumerable())
        {
            if (key.EndsWith(":path", StringComparison.OrdinalIgnoreCase)
                && value is not null
                && (value == "~" || value.StartsWith("~/") || value.StartsWith("~\\")))
            {
                configuration[key] = Path.Combine(home, value[1..].TrimStart('/', '\\'));
            }
        }
    }
}