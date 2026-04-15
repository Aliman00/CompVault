using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.JobTitles;

/// <summary>
/// En definert stillingstittel i systemet. Brukes for å sikre konsistente
/// jobbtitler på tvers av brukere og dokumentmålsetting.
/// </summary>
public class JobTitle
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>Navn på stillingstittelen, f.eks. "Systemutvikler".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Om stillingstittelen er aktiv (ikke slettet).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når stillingstittelen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Når stillingstittelen ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    /// <summary>Brukere med denne stillingstittelen.</summary>
    public ICollection<ApplicationUser> Users { get; set; } = new List<ApplicationUser>();
}