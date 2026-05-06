using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.Auth;

/// <summary>
/// Et refresh token brukt til å utstede nye access tokens uten ny innlogging.
/// </summary>
public class RefreshToken
{
    // ======================== Primary Key ========================
    public Guid Id { get; set; }

    // ======================== Foreign Key ========================
    /// <summary>
    /// Brukeren som eier dette refresh tokenet.
    /// </summary>
    public Guid UserId { get; set; }

    // ======================== Egenskaper ========================
    /// <summary>
    /// Selve token-strengen (Base64, 64 bytes fra RandomNumberGenerator).
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Når tokenet ble opprettet.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Når tokenet går ut.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Satt til true når tokenet er tilbakekalt manuelt (logout/revoke).
    /// </summary>
    public bool IsRevoked { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    public ApplicationUser User { get; set; } = null!;
}