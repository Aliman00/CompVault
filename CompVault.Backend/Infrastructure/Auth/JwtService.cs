using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.Constants;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CompVault.Backend.Infrastructure.Auth;

/// <summary>
/// Implementerer <see cref="IJwtService"/> med HMAC-SHA256 signering.
/// </summary>
public sealed class JwtService(IOptions<JwtSettings> settings) : IJwtService
{
    /// <inheritdoc />
    public string GenerateAccessToken(ApplicationUser user, IEnumerable<string> roles, IEnumerable<string> permissions)
    {
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(settings.Value.Secret));
        SigningCredentials credentials = new(key, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("lastName", user.LastName)
        ];

        // Legger til avadelingsID i claim for å enkelt sjekke brukerens tilattelse i hierarkiet
        claims.Add(new Claim("department_id", user.DepartmentId.ToString()));

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        claims.AddRange(permissions.Select(p => new Claim(Permissions.ClaimType, p)));

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
    public int RefreshTokenLifetimeDays => settings.Value.RefreshTokenDays;
}