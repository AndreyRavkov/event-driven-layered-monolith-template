using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AR.Infrastructure.Jwt.DTO;
using AR.Shared.Options;
using Microsoft.IdentityModel.Tokens;

namespace AR.Infrastructure.Jwt;

public interface IJwtTokenProvider
{
    string Create(JwtTokenDtoModel model);
}

internal class JwtTokenProvider : IJwtTokenProvider
{
    private readonly JwtSettings _jwtSettings;

    public JwtTokenProvider(JwtSettings jwtSettings) => _jwtSettings = jwtSettings;

    public string Create(JwtTokenDtoModel model)
    {
        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSettings.Secret));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha512Signature);
        var tokenExpires = DateTime.UtcNow.AddMinutes(_jwtSettings.ExpireMinutes);

        var tokenOptions = new JwtSecurityToken(
            _jwtSettings.Issuer,
            _jwtSettings.Secret,
            new List<Claim>
            {
                new(ClaimTypes.Sid, model.UserId.ToString()),
                new(ClaimTypes.Name, model.UserName),
            },
            expires: tokenExpires,
            signingCredentials: credentials);
        var tokenValue = new JwtSecurityTokenHandler().WriteToken(tokenOptions);
        return tokenValue;
    }
}