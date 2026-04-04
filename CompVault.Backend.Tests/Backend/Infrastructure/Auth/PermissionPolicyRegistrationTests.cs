using System.Reflection;
using System.Security.Claims;

using CompVault.Backend.Infrastructure.Auth;
using CompVault.Backend.Infrastructure.Extensions;
using CompVault.Shared.Constants;

using FluentAssertions;

using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace CompVault.Backend.Tests.Backend.Infrastructure.Auth;

/// <summary>
/// Tester at policy-registrering via reflection i ServiceCollectionExtensions.AddAuth
/// fungerer korrekt - dvs at alle Permissions.cs-konstanter har en tilsvarende
/// policy som krever korrekt claim.
/// </summary>
public class PermissionPolicyRegistrationTests
{
    /// <summary>
    /// Verifiserer at alle permission-konstanter i Permissions.cs
    /// blir registrert som policies via reflection.
    /// </summary>
    [Fact]
    public void AllPermissionConstants_HaveCorrespondingPolicy()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());

        var expectedPermissions = typeof(Permissions)
            .GetFields(BindingFlags.Public | BindingFlags.Static | BindingFlags.DeclaredOnly)
            .Where(f => f.FieldType == typeof(string))
            .Select(f => (string)f.GetValue(null)!)
            .ToList();

        // Act
        services.AddAuth(ConfigurationStub());
        ServiceProvider provider = services.BuildServiceProvider();

        // Assert
        using var scope = provider.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();
        authorizationService.Should().NotBeNull();

        // Verifiser at hver permission kan autoriseres når brukeren har korrekt claim
        foreach (string permission in expectedPermissions)
        {
            var claims = new[] { new Claim(Permissions.ClaimType, permission) };
            var identity = new ClaimsIdentity(claims, "Test");
            var principal = new ClaimsPrincipal(identity);

            var result = authorizationService.AuthorizeAsync(principal, permission).Result;
            result.Succeeded.Should().BeTrue(
                $"Policy '{permission}' should be registered and authorize when user has the correct claim");
        }
    }

    /// <summary>
    /// Verifiserer at policy krever korrekt claim-type og verdi.
    /// </summary>
    [Fact]
    public void PolicyRequiresCorrectClaim()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging(builder => builder.AddConsole());
        services.AddAuth(ConfigurationStub());
        ServiceProvider provider = services.BuildServiceProvider();

        using var scope = provider.CreateScope();
        var authorizationService = scope.ServiceProvider.GetRequiredService<IAuthorizationService>();

        // Act & Assert - Policy godkjenner kun når claim-type OG verdi matcher
        var validClaims = new[] { new Claim(Permissions.ClaimType, Permissions.RolesRead) };
        var validIdentity = new ClaimsIdentity(validClaims, "Test");
        var validPrincipal = new ClaimsPrincipal(validIdentity);

        var successResult = authorizationService.AuthorizeAsync(validPrincipal, Permissions.RolesRead).Result;
        successResult.Succeeded.Should().BeTrue();

        // Feil claim-verdi skal ikke autoriseres
        var wrongClaims = new[] { new Claim(Permissions.ClaimType, "wrong:permission") };
        var wrongIdentity = new ClaimsIdentity(wrongClaims, "Test");
        var wrongPrincipal = new ClaimsPrincipal(wrongIdentity);

        var failResult = authorizationService.AuthorizeAsync(wrongPrincipal, Permissions.RolesRead).Result;
        failResult.Succeeded.Should().BeFalse();
    }

    private static IConfiguration ConfigurationStub()
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                [$"{JwtSettings.SectionName}:{nameof(JwtSettings.Issuer)}"] = "test-issuer",
                [$"{JwtSettings.SectionName}:{nameof(JwtSettings.Audience)}"] = "test-audience",
                [$"{JwtSettings.SectionName}:{nameof(JwtSettings.Secret)}"] = "test-secret-key-that-is-at-least-32-chars-long",
            })
            .Build();
    }
}
