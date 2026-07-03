using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AjoVault.API.Config;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace AjoVault.API.Auth;

public class JwtService(IOptions<JwtSettings> jwtOptions)
{
    private readonly JwtSettings _jwt = jwtOptions.Value;

    public string GenerateToken(Guid userId, string email)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwt.Secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim("userId", userId.ToString()),
            new Claim(ClaimTypes.Email, email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: _jwt.Issuer,
            audience: _jwt.Audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_jwt.ExpiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
