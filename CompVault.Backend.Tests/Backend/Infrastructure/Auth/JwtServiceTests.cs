using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Auth;

using FluentAssertions;

using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace CompVault.Backend.Tests.Backend.Infrastructure.Auth;

public class JwtServiceTests
{
    // Systemet vi tester
    private readonly JwtService _sut;

    // Testbruker som gjenbrukes på tvers av testene
    private readonly ApplicationUser _testUser = new()
    {
        Id = Guid.NewGuid(),
        Email = "test@example.com",
        FirstName = "Ola",
        LastName = "Nordmann"
    };

    // Test-innstillinger for JWT
    private static readonly JwtSettings JwtSettings = new()
    {
        Secret = "super-secret-key-som-er-lang-nok-til-hmac-256",
        Issuer = "compvault-test",
        Audience = "compvault-test-audience",
        AccessTokenMinutes = 15,
        RefreshTokenDays = 7
    };

    public JwtServiceTests()
    {
        _sut = new JwtService(Options.Create(JwtSettings), NullLogger<JwtService>.Instance);
    }

    /// <summary>
    /// Tester at GenerateAccessToken lager et token med riktige claims
    /// (userId, email, firstName, lastName og rolle)
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithValidUser_ContainsCorrectClaims()
    {
        // Arrange
        string[] roles = new[] { "Admin" };

        // Act
        string token = _sut.GenerateAccessToken(_testUser, roles, []);
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken parsed = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(_testUser.Id.ToString(), parsed.Subject);
        Assert.Equal(_testUser.Email, parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.Equal("Ola", parsed.Claims.First(c => c.Type == "firstName").Value);
        Assert.Equal("Nordmann", parsed.Claims.First(c => c.Type == "lastName").Value);
        Assert.Contains(parsed.Claims, c => c.Type == ClaimTypes.Role && c.Value == "Admin");
    }

    /// <summary>
    /// Tester at GenerateAccessToken setter riktig issuer og audience
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithValidUser_HasCorrectIssuerAndAudience()
    {
        // Act
        string token = _sut.GenerateAccessToken(_testUser, [], []);
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken parsed = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(JwtSettings.Issuer, parsed.Issuer);
        Assert.Contains(JwtSettings.Audience, parsed.Audiences);
    }



    /// <summary>
    /// Tester at GenerateAccessToken setter korrekt utløpstidspunkt basert på AccessTokenMinutes
    /// </summary>
    [Fact]
    public void GenerateAccessToken_HasCorrectExpiration()
    {
        // Arrange
        DateTime before = DateTime.UtcNow;

        // Act
        string token = _sut.GenerateAccessToken(_testUser, [], []);
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken parsed = handler.ReadJwtToken(token);

        // Assert — ValidTo skal være innenfor ett sekund av forventet utløpstidspunkt
        DateTime expectedExpiry = before.AddMinutes(JwtSettings.AccessTokenMinutes);
        parsed.ValidTo.Should().BeCloseTo(expectedExpiry, TimeSpan.FromSeconds(1));
    }

    /// <summary>
    /// Tester at GetPrincipalFromExpiredToken klarer å lese claims korrekt.
    /// Metoden bruker ValidateLifetime = false internt, så den fungerer
    /// uavhengig av om tokenet er utløpt eller ikke.
    /// </summary>
    [Fact]
    public void GetPrincipalFromExpiredToken_ReturnsPrincipalWithClaims()
    {
        // Arrange - Generer et normalt token — GetPrincipalFromExpiredToken
        // validerer uansett med ValidateLifetime = false, så det holder
        string token = _sut.GenerateAccessToken(_testUser, [], []);

        // Act
        ClaimsPrincipal? principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert - Skal kunne lese claims uavhengig av levetid
        Assert.NotNull(principal);
        Assert.Equal(_testUser.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }


    /// <summary>
    /// Tester at GetPrincipalFromExpiredToken returnerer null når tokenet er tuklet med
    /// </summary>
    [Fact]
    public void GetPrincipalFromExpiredToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        string validToken = _sut.GenerateAccessToken(_testUser, [], []);
        string tamperedToken = validToken[..^5] + "XXXXX"; // Ødelegger signaturen

        // Act
        ClaimsPrincipal? principal = _sut.GetPrincipalFromExpiredToken(tamperedToken);

        // Assert
        Assert.Null(principal);
    }

    /// <summary>
    /// Tester at GenerateAccessToken legger til permission-claims korrekt
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithPermissions_ContainsPermissionClaims()
    {
        // Arrange
        string[] roles = ["Admin"];
        string[] permissions = ["users:read", "users:write", "departments:read"];

        // Act
        string token = _sut.GenerateAccessToken(_testUser, roles, permissions);
        var handler = new JwtSecurityTokenHandler();
        JwtSecurityToken parsed = handler.ReadJwtToken(token);

        // Assert
        var permissionClaims = parsed.Claims
            .Where(c => c.Type == "permission")
            .Select(c => c.Value)
            .ToList();
        
        permissionClaims.Should().HaveCount(3);
        permissionClaims.Should().Contain("users:read");
        permissionClaims.Should().Contain("users:write");
        permissionClaims.Should().Contain("departments:read");
    }
}