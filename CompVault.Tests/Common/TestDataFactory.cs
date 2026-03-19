using CompVault.Backend.Common.Security;
using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Tests.Common.Constants;

namespace CompVault.Tests.Common;

/// <summary>
/// For opprettelse av database modell-objekter for testing
/// </summary>
public static class TestDataFactory
{
    /// <summary>
    /// Oppretter en ApplicationUser for testing. Brukes i de fleste testene.
    /// Hvis deletedAt har en verdi, så er brukeren inaktive/slettet
    /// Guid er optional. Bruker ActiveUserId som default hvis ingen annen informasjon er oppgitt
    /// </summary>
    /// <param name="id">ID til en bruker hvis man trenger å slå opp ID for testing</param>
    /// <param name="email">Optional string med Epost for å opprette forskjellige brukere</param>
    /// <param name="deletedAt">DateTime som bestemmer om brukeren er aktive/slettet</param>
    /// <returns>En ferdig opprettet ApplicationUser for testing</returns>
    public static ApplicationUser CreateApplicationUser(Guid? id = null,
        string email = TestConstants.Users.DefaultEmailForActiveUser, DateTime? deletedAt = null) => new()
    {
        Id = id ?? Guid.NewGuid(),
        Email = email,
        UserName = email,
        FirstName = "Fredrik",
        LastName = "Magee",
        IsActive = deletedAt == null,
        DeletedAt = deletedAt
    };

    /// <summary>
    /// Oppretter en Otp-kode tilhørende en bruker
    /// </summary>
    /// <param name="userId">Brukeren som Otp-koden tilhører</param>
    /// <param name="plainTextCode">Koden i plaintext som blir hashet i metoden</param>
    /// <param name="createdAt">Når OTP-koden er opprettet</param>
    /// <param name="expiresAt">DateTime-objekt som spesifiserer når den går ut</param>
    /// <param name="failedAttempts">Antall feilede forsøk</param>
    /// <param name="isUsed">Setter om OTP-koden er brukt eller ikke</param>
    /// <returns>En opprettet OtpCode</returns>
    public static OtpCode CreateOtpCode(Guid? userId = null, string plainTextCode = TestConstants.Otp.PlainTextOtpCode,
        DateTime? createdAt = null, DateTime? expiresAt = null, int failedAttempts = 0, bool isUsed = false) => new()
    {
        UserId = userId ?? TestConstants.Users.ActiveUserId,
        Code = OtpHasher.HashCode(plainTextCode),
        CreatedAt = createdAt ?? DateTime.UtcNow,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(10),
        FailedAttempts = failedAttempts,
        IsUsed = isUsed,
    };
    
    /// <summary>
    /// Oppretter en RefreshToken tilhørende en bruker
    /// </summary>
    /// <param name="userId">Brukeren som Token tilhører. Default ActiveUserId</param>
    /// <param name="token">Selve token, kun en enkel string i testene. Default token-konstant</param>
    /// <param name="createdAt">Når den er opprettet. Default UtcNow</param>
    /// <param name="expiresAt">Når den utgår. Default om 15 min fra opprettelse</param>
    /// <param name="isRevoked">Bool på om koden er gyldig eller revoked</param>
    /// <returns>En opprettet RefreshToken</returns>
    public static RefreshToken CreateRefreshToken(Guid? userId = null, string token = TestConstants.RefreshToken.Token,
        DateTime? createdAt = null, DateTime? expiresAt = null, bool isRevoked = false) => new()
    {
        UserId = userId ?? TestConstants.Users.ActiveUserId,
        Token = token,
        CreatedAt = createdAt ?? DateTime.UtcNow,
        ExpiresAt = expiresAt ?? DateTime.UtcNow.AddMinutes(15),
        IsRevoked = isRevoked
    };
}