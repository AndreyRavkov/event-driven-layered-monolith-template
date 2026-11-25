namespace AR.API.Infrastructure.Middlewares;

internal static class UseCustomMiddlewares
{
    internal static IApplicationBuilder UseCustomMiddlewaresBeforeAuthExt(this IApplicationBuilder app) =>
        app.UseMiddleware<CorrelationIdMiddleware>();

    internal static IApplicationBuilder UseCustomMiddlewaresExt(this IApplicationBuilder app) =>
        // here apply list of custom middlewares
        app;
}