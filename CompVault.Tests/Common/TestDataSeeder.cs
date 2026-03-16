using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;
using CompVault.Tests.Common.Constants;
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
    public static ApplicationUser CreateApplicationUser(string email = TestConstants.Users.DefaultEmailForActiveUser, 
        DateTime? deletedAt = null) => new()
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
    /// Sletter en eksisterende database, og oppretter en ny database mellom hver integrasjonstest
    /// Legger til en aktiv og en inaktiv bruker ved oppstart
    /// </summary>
    public static async Task CreateDbAndSeedUsersAsync(IServiceProvider serviceProvider)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        
        // Nuker databasen og oppretter en ny database for hver integrasjonstest
        await context.Database.EnsureDeletedAsync();
        await context.Database.EnsureCreatedAsync();
        
        // Seeder en aktiv og en inaktiv bruker
        await userManager.CreateAsync(CreateApplicationUser());
        await userManager.CreateAsync(CreateApplicationUser(email: TestConstants.Users.DefaultEmailForInactiveUser, 
            deletedAt: DateTime.UtcNow));
    }
}