namespace AR.API.Infrastructure.ExceptionHandlers;

internal static class ConfigureExceptionHandlersExtensions
{
    internal static IServiceCollection AddExceptionHandlerExt(this IServiceCollection services)
    {
        services.AddExceptionHandler<GlobalExceptionHandler>();
        services.AddProblemDetails();
        
        return services;
    }

    internal static IApplicationBuilder UseExceptionHandlerExt(this IApplicationBuilder app)
    {
        app.UseExceptionHandler();

        return app;
    }
}