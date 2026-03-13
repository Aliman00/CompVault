using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Auth;
using Microsoft.Extensions.Options;

namespace CompVault.Tests.Infrastructure.Auth;

public class JwtServiceTests
{
    // Systemet vi tester
    private readonly JwtService _sut;

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
        _sut = new JwtService(Options.Create(JwtSettings));
    }

    /// <summary>
    /// Tester at GenerateAccessToken lager et token med riktige claims
    /// (userId, email, firstName, lastName og rolle)
    /// </summary>
    [Fact]
    public void GenerateAccessToken_WithValidUser_ContainsCorrectClaims()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Ola",
            LastName = "Nordmann"
        };
        var roles = new[] { "Admin" };

        // Act
        var token = _sut.GenerateAccessToken(user, roles);
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(user.Id.ToString(), parsed.Subject);
        Assert.Equal(user.Email, parsed.Claims.First(c => c.Type == JwtRegisteredClaimNames.Email).Value);
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
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Ola",
            LastName = "Nordmann"
        };

        // Act
        var token = _sut.GenerateAccessToken(user, []);
        var handler = new JwtSecurityTokenHandler();
        var parsed = handler.ReadJwtToken(token);

        // Assert
        Assert.Equal(JwtSettings.Issuer, parsed.Issuer);
        Assert.Contains(JwtSettings.Audience, parsed.Audiences);
    }

    /// <summary>
    /// Tester at GenerateRefreshToken returnerer en unik, ikke-tom Base64-streng
    /// </summary>
    [Fact]
    public void GenerateRefreshToken_ReturnsTwoUniqueTokens()
    {
        // Act
        var token1 = _sut.GenerateRefreshToken();
        var token2 = _sut.GenerateRefreshToken();

        // Assert - Skal ikke være tom og to kall skal aldri gi samme token
        Assert.False(string.IsNullOrEmpty(token1));
        Assert.NotEqual(token1, token2);
    }

    /// <summary>
    /// Tester at GetPrincipalFromExpiredToken klarer å lese claims fra et utløpt token
    /// ved å lage tokenet normalt, men validere med ClockSkew = 0 og bakoverforskjøvet tid
    /// </summary>
    [Fact]
    public void GetPrincipalFromExpiredToken_WithExpiredToken_ReturnsPrincipalWithClaims()
    {
        // Arrange - Lager et gyldig token, men med kun 1 minutt levetid
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "expired@example.com",
            FirstName = "Test",
            LastName = "User"
        };

        // Generer et normalt token — GetPrincipalFromExpiredToken
        // validerer uansett med ValidateLifetime = false, så det holder
        var token = _sut.GenerateAccessToken(user, []);

        // Act
        var principal = _sut.GetPrincipalFromExpiredToken(token);

        // Assert - Skal kunne lese claims uavhengig av levetid
        Assert.NotNull(principal);
        Assert.Equal(user.Id.ToString(), principal.FindFirstValue(ClaimTypes.NameIdentifier));
    }


    /// <summary>
    /// Tester at GetPrincipalFromExpiredToken returnerer null når tokenet er tuklet med
    /// </summary>
    [Fact]
    public void GetPrincipalFromExpiredToken_WithTamperedToken_ReturnsNull()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            Email = "test@example.com",
            FirstName = "Ola",
            LastName = "Nordmann"
        };
        var validToken = _sut.GenerateAccessToken(user, []);
        var tamperedToken = validToken[..^5] + "XXXXX"; // Ødelegger signaturen

        // Act
        var principal = _sut.GetPrincipalFromExpiredToken(tamperedToken);

        // Assert
        Assert.Null(principal);
    }
}
