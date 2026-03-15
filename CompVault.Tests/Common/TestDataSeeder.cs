using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CompVault.Tests.Common;

/// <summary>
/// Oppretter ApplicationUsers for testing, og seeder inne i InMemory-databaser
/// </summary>
public static class TestDataSeeder
{
    /// <summary>
    /// Oppretter en ApplicationUser for testing. Brukes i de fleste testene.
    /// Hvis deletedAt har en verdi, så er brukeren inaktive/slettet
    /// </summary>
    /// <param name="email">Optional string med Epost for å opprette forskjellige brukere</param>
    /// <param name="deletedAt">DateTime som bestemmer om brukeren er aktive/slettet</param>
    /// <returns>En ferdig opprettet ApplicationUser for testing</returns>
    public static ApplicationUser CreateApplicationUser(string email = "test@compvault.no", DateTime? deletedAt = null)
        => new()
    {
        Id = Guid.NewGuid(),
        Email = email,
        UserName = email,
        FirstName = "Fredrik",
        LastName = "Magee",
        IsActive = deletedAt == null,
        DeletedAt = deletedAt
    };
    
    /// <summary>
    /// Legger til en aktiv og en inaktiv bruker ved oppstart av integrasjonstestene
    /// </summary>
    public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Seeder en aktiv og en inaktiv bruker
        await userManager.CreateAsync(CreateApplicationUser());
        await userManager.CreateAsync(CreateApplicationUser(email: "test2@testing.no", deletedAt: DateTime.UtcNow));
    }
    
    /// <summary>
    /// Rydder opp i databasen etter kjøring. Flere integrasjonstester, så må v
    /// </summary>
    public static async Task ClearDatabaseAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context  = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        
        // Nuker databasen
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
    }
}