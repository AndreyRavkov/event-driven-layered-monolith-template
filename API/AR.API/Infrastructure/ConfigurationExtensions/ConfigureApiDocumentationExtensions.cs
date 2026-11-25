using Microsoft.OpenApi;
using Scalar.AspNetCore;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureApiDocumentationExtensions
{
    internal static IServiceCollection ConfigureOpenApiExt(this IServiceCollection services)
    {
        services.AddOpenApi(
            $"v{ApiConstants.ApiVersion1}",
            options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
        services.AddOpenApi(
            $"v{ApiConstants.ApiVersion2}",
            options => options.OpenApiVersion = OpenApiSpecVersion.OpenApi3_1);
        return services;
    }

    internal static void UseOpenApiExt(this WebApplication app)
    {
        app.MapOpenApi();

        app.MapScalarApiReference(ApiConstants.ApiDocUrl, options =>
        {
            options
                .WithTitle("API")
                .WithTheme(ScalarTheme.Default)
                .SortTagsAlphabetically()
                .SortOperationsByMethod()
                .HideClientButton()
                .HideDarkModeToggle();

            options
                .AddDocument($"v{ApiConstants.ApiVersion1}", "API v1", "/openapi/v1.json", isDefault: true)
                .AddDocument($"v{ApiConstants.ApiVersion2}", "API v2", "/openapi/v2.json");
        });
    }
}
