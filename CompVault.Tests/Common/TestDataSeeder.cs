using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Tests.Common;

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
}