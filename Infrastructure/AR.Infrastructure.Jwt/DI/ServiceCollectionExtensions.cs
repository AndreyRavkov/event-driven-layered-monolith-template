using Microsoft.Extensions.DependencyInjection;

namespace AR.Infrastructure.Jwt.DI;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddJwtTokenProvider(this IServiceCollection services)
    {
        services.AddScoped<IJwtTokenProvider, JwtTokenProvider>();
        return services;
    }
}