using System.Text;
using AR.API.Infrastructure.AuthHandlers;
using AR.Shared.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;

namespace AR.API.Infrastructure.ConfigurationExtensions;

internal static class ConfigureAuthorizationExtensions
{
    internal static IServiceCollection ConfigureAuthorizationExt(
        this IServiceCollection services,
        ConfigurationManager configurationManager)
    {
        var jwtSettings = configurationManager.GetOptionsExt<JwtSettings>(JwtSettings.Name);

        if (string.IsNullOrEmpty(jwtSettings.Issuer))
        {
            throw new NullReferenceException(nameof(jwtSettings.Issuer));
        }
        if (string.IsNullOrEmpty(jwtSettings.Secret))
        {
            throw new NullReferenceException(nameof(jwtSettings.Secret));
        }

        services
            .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(o => o.TokenValidationParameters = new TokenValidationParameters()
                {
                    ValidateIssuer = true,
                    ValidateAudience = false,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings.Issuer,
                    ValidAudience = jwtSettings.Issuer,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.Secret)),
                    ClockSkew = TimeSpan.Zero,
                });

        services.AddAuthorizationBuilder()
            .SetInvokeHandlersAfterFailure(false)
            .AddPolicy(AccessPolicyHandler.Name,
                       policy => policy
                               .RequireAuthenticatedUser()
                               .AddRequirements(new AccessPolicyRequirement()));

        return services;
    }
}
