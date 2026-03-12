using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.Auth;

/// <summary>
/// En OTP-kode som brukes for innlogging
/// </summary>
public class OtpCode
{
    // ======================== Primary Key ========================
    public Guid Id { get; set; }
    
    // ======================== Foreign Key ========================
    /// <summary>
    /// Brukeren som får koden
    /// </summary>
    public Guid UserId { get; set; }
    
    // ======================== Egenskaper ========================
    /// <summary>
    /// HA-256 hash (64 tegn, hex) av den 6-sifrede OTP-koden
    /// </summary>
    public string Code { get; set; } = string.Empty; 
    
    /// <summary>
    /// Når koden er opprettet
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>
    /// Når koden går ut
    /// </summary>
    public DateTime ExpiresAt { get; set; }
    
    /// <summary>
    /// IsUsed er true når koden er brukt, og false hvis ikke
    /// </summary>
    public bool IsUsed { get; set; }
    
    // ======================== Beregnet  ========================
    /// <summary>
    /// Koden er gyldig hvis den ikke er brukt og ikke er utgått
    /// </summary>
    public bool IsValid => !IsUsed && ExpiresAt > DateTime.UtcNow;
    
    
    // ======================== Feil kodeforsøk ========================

    /// <summary>
    /// Antall forsøk på å skrive korrekt kode
    /// </summary>
    public int FailedAttempts { get; set; } = 0;
    
    /// <summary>
    /// Antall forsøk på å skrive korrekt kode
    /// </summary>
    public DateTime? LastAttemptAt { get; set; }
    
    // ======================== Navigasjonsegenskaper ========================
    public ApplicationUser User { get; set; } = null!;
    
    
}