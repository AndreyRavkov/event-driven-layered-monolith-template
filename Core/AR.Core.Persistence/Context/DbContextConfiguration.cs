using AR.Core.Persistence.Interceptors;
using AR.Infrastructure.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace AR.Core.Persistence.Context;

internal static class DbContextConfiguration
{
    internal static void LoadDbContext(IServiceCollection services, string mainDb) =>
        services.AddDbContext<DbDomainContext>((serviceProvider, options) =>
        {
            options
                //.UseSqlServer(mainDb)
                .UseSqlite(mainDb)
                .AddInterceptors(
                    serviceProvider.GetRequiredService<UpdateAuditableEntityInterceptor>());

            if (AREnvironment.IsLocal())
            {
                options.EnableSensitiveDataLogging();
                options.LogTo(Console.WriteLine, LogLevel.Information);
            }
        });
}