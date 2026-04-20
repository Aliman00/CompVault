using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using CompVault.Frontend.Common.Http;
using CompVault.Shared.Constants;

using FluentAssertions;

using Microsoft.IdentityModel.Tokens;

namespace CompVault.Frontend.Tests.Frontend.Common.Http;

public class ClaimsSynchronizerTests
{
    // -------------------------------------------------------------------------
    // Hjelpemetoder
    // -------------------------------------------------------------------------

    /// <summary>
    /// Oppretter en JWT-token for å teste metoden med ekte JWT-token
    /// </summary>
    /// <param name="claims">Claims for å tilpasse testene med forskjellige claims</param>
    /// <returns>JWT-token</returns>
    private static string BuildJwt(IEnumerable<Claim> claims)
    {
        // Oppretter en signeringsnøkkel for å signere token - minimum 16 bytes
        var key = new SymmetricSecurityKey("test-secret-key-minimum-16-bytes"u8.ToArray());
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var token = new JwtSecurityToken(
            issuer: "testIssuer",
            audience: "testAudience",
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(15),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    /// <summary>
    /// Oppretter en ClaimsIdentity med en liste med claims
    /// </summary>
    private static ClaimsIdentity BuildIdentity(IEnumerable<Claim> claims) => new(claims, "Cookie");

    // -------------------------------------------------------------------------
    // Happy Paths
    // -------------------------------------------------------------------------

    /// <summary>
    /// Tester at RefreshClaimsFromAccessToken bytter claim Email  av samme type, og ikke legger til flere like claims
    /// </summary>
    [Fact]
    public void RefreshClaimsFromAccessToken_EmailChanged_ReplacesEmailClaim()
    {
        // Arrange - Bygger claims og gir gammel til identity og den nye til Jwt
        var oldEmailClaim = new Claim(ClaimTypes.Email, "oldEmail@test.no");
        var newEmailClaim = new Claim(ClaimTypes.Email, "newEmail@test.no");
        ClaimsIdentity identity = BuildIdentity([oldEmailClaim]);
        string jwt = BuildJwt([newEmailClaim]);

        // Act
        ClaimsSynchronizer.RefreshClaimsFromAccessToken(identity, jwt);

        // Assert
        identity.FindAll(ClaimTypes.Email).Should().ContainSingle()
            .Which.Value.Should().Be("newEmail@test.no");
    }

    /// <summary>
    /// Tester at roller blir byttet korrekt og det er riktig antall roller etter et bytte
    /// Viktig test som sikrer at bruker får roller i sanntid etter endring
    /// </summary>
    [Fact]
    public void RefreshClaimsFromAccessToken_RolesChanged_ReplacesRoleClaim()
    {
        // Arrange
        var oldRoleClaim1 = new Claim(ClaimTypes.Role, "Admin");
        var oldRoleClaim2 = new Claim(ClaimTypes.Role, "Manager");
        var newRoleClaim = new Claim(ClaimTypes.Role, "Employee");

        ClaimsIdentity identity = BuildIdentity([oldRoleClaim1, oldRoleClaim2]);
        string jwt = BuildJwt([newRoleClaim]);

        // Act
        ClaimsSynchronizer.RefreshClaimsFromAccessToken(identity, jwt);

        // Assert
        identity.FindAll(ClaimTypes.Role).Should().ContainSingle()
            .Which.Value.Should().Be("Employee");
    }

    /// <summary>
    /// Tester at alle permissions blir korrekt oppdatert. Har med en gammel permission og 2 nye.
    /// Viktig test som sikrer at en bruker slipper å vente 15 min/relogge etter nye permissions
    /// </summary>
    [Fact]
    public void RefreshClaimsFromAccessToken_PermissionsAdded_ReplacesPermissionClaim()
    {
        // Arrange
        var oldPermissionClaim = new Claim(Permissions.ClaimType, "users:read");
        var newPermissionClaim1 = new Claim(Permissions.ClaimType, "users:read");
        var newPermissionClaim2 = new Claim(Permissions.ClaimType, "users:write");
        var newPermissionClaim3 = new Claim(Permissions.ClaimType, "users:delete");

        ClaimsIdentity identity = BuildIdentity([oldPermissionClaim]);
        string jwt = BuildJwt([newPermissionClaim1, newPermissionClaim2, newPermissionClaim3]);

        // Act
        ClaimsSynchronizer.RefreshClaimsFromAccessToken(identity, jwt);

        // Assert
        identity.FindAll(Permissions.ClaimType)
            .Select(c => c.Value)
            .Should().BeEquivalentTo("users:read", "users:write", "users:delete");
    }

    /// <summary>
    /// Tester at andre claims (som Sub og JTI) ikke blir rørt av metoden. Det er ikke denne metoden sitt ansvar
    /// </summary>
    [Fact]
    public void RefreshClaimsFromAccessToken_OtherClaims_NotModified()
    {
        // Arrange - Bruker ID-en i forskjellige i gammel og ny claim
        string originalUserId = Guid.NewGuid().ToString();
        string differentUserId = Guid.NewGuid().ToString();
        var oldSubClaim = new Claim("sub", originalUserId);
        var newSubClaim = new Claim("sub", differentUserId);

        ClaimsIdentity identity = BuildIdentity([oldSubClaim]);
        string jwt = BuildJwt([newSubClaim]);

        // Act
        ClaimsSynchronizer.RefreshClaimsFromAccessToken(identity, jwt);

        // Assert - Ingen endring av sub
        identity.FindFirst("sub")?.Value.Should().Be(originalUserId);
    }

    // -------------------------------------------------------------------------
    // Failure paths
    // -------------------------------------------------------------------------

    /// <summary>
    /// Teester at invalid Jwt ikke endrer de gamle claimene. Det er ikke denne metoden sitt ansvar å logge brukeren ut
    /// </summary>
    [Fact]
    public void RefreshClaimsFromAccessToken_InvalidToken_LeavesIdentityUnchanged()
    {
        // Arrange - Bygger identity med en EmailClaim, men ingen Jwt
        var emailClaim = new Claim(ClaimTypes.Email, "email@test.no");
        ClaimsIdentity identity = BuildIdentity([emailClaim]);

        // Act
        ClaimsSynchronizer.RefreshClaimsFromAccessToken(identity, "not-a-jwt");

        // Assert - Claimene er ikke endret
        identity.FindFirst(ClaimTypes.Email)?.Value.Should().Be("email@test.no");
    }

}