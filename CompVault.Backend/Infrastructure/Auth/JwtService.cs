using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using CompVault.Backend.Domain.Entities.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Implementerer <see cref="IJwtService"/> med HMAC-SHA256 signering.
/// </summary>
public sealed class JwtService(IOptions<JwtSettings> settings) : IJwtService
{
    /// <inheritdoc />
    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(settings.Value.Secret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims = new()
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        
        // Setter en DateTime til nå slik at notBefore og expires får samme tid
        DateTime now = DateTime.UtcNow;
        
        JwtSecurityToken token = new(
            issuer: settings.Value.Issuer,
            audience: settings.Value.Audience,
            claims: claims,
            notBefore: now,
            expires: now.AddMinutes(settings.Value.AccessTokenMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <inheritdoc />
    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        TokenValidationParameters parameters = new()
        {
            ValidateIssuer = true,
            ValidIssuer = settings.Value.Issuer,
            ValidateAudience = true,
            ValidAudience = settings.Value.Audience,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(settings.Value.Secret)),
            ValidateLifetime = false // allow expired tokens for refresh flow
        };

        JwtSecurityTokenHandler handler = new();

        try
        {
            ClaimsPrincipal principal = handler.ValidateToken(token, parameters, out SecurityToken securityToken);

            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            return principal;
        }
        catch
        {
            return null;
        }
    }
}
