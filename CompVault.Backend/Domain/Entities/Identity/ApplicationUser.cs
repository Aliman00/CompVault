using CompVault.Backend.Domain.Entities.Auth;
using CompVault.Shared.Enums;
using Microsoft.AspNetCore.Identity;

namespace CompVault.Backend.Domain.Entities.Identity;

/// <summary>
/// En bruker i systemet. Bygger videre på Identity sin standard brukerklasse
/// med ekstra felt vi trenger for CompVault.
/// </summary>
public class ApplicationUser : IdentityUser<Guid>
{
    // IdentityUser egenskaper:
    // - Id (Guid)
    // - UserName (string)
    // - Email (string)
    // - EmailConfirmed (bool)
    // - PasswordHash (string)
    // - PhoneNumber (string)
    // - PhoneNumberConfirmed (bool)
    // - TwoFactorEnabled (bool)
    // - LockoutEnd (DateTimeOffset?)
    // - LockoutEnabled (bool)
    // - AccessFailedCount (int)

    // ======================== User egenskaper ========================
    /// <summary>Fornavn.</summary>
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Etternavn.</summary>
    public string LastName { get; set; } = string.Empty;

    /// <summary>Stillingstittel, f.eks. "Systemutvikler".</summary>
    public string JobTitle { get; set; } = string.Empty;

    /// <summary>Om brukeren er fast ansatt, midlertidig eller innleid.</summary>
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    // ======================== Aktive/slettet ========================

    /// <summary>Om kontoen er aktiv. Er true som standard.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Satt når brukeren er slettet (soft delete). Null = ikke slettet.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Historikk ========================
    /// <summary>Når brukeren ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // ======================== Foreign keys ========================
    /// <summary>ID til nærmeste leder. Peker tilbake på samme tabell.</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Hvilken avdeling brukeren tilhører.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Brukeren som opprettet brukeren</summary>
    public Guid? CreatedById { get; set; }


    // ======================== Navigasjonsegenskaper ========================
    public ApplicationUser? Manager { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public Department? Department { get; set; }
    public ICollection<ApplicationUser> DirectReports { get; set; } = new List<ApplicationUser>();
    public ICollection<OtpCode> OtpCodes { get; set; } = new List<OtpCode>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
}
