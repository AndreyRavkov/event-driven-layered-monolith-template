using System.Net;
using Asp.Versioning;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureApiVersionsExtensions
{
    internal static IServiceCollection ConfigureApiVersionsExt(this IServiceCollection services)
    {
        services
            .AddApiVersioning(o =>
            {
                o.ApiVersionReader = ApiVersionReader.Combine(new UrlSegmentApiVersionReader());
                o.UnsupportedApiVersionStatusCode = (int)HttpStatusCode.NotAcceptable;
                o.ReportApiVersions = true;
                o.AssumeDefaultVersionWhenUnspecified = false;
                o.ApiVersionSelector = new CurrentImplementationApiVersionSelector(o);
            })
            .AddMvc()
            .AddApiExplorer(o =>
            {
                o.GroupNameFormat = "'v'VV";
                o.SubstituteApiVersionInUrl = true;
            });

        return services;
    }
}
