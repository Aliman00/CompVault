using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Spesialisert DTO for utløpende og utløpte kompetansebevis.
/// Brukes av GET /api/competencies/expiring.
/// Inkluderer avdelingsinfo for gruppering i frontend.
/// </summary>
public sealed class ExpiringCompetencyDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Navn på kompetansetypen.</summary>
    public string? CompetencyTypeName { get; set; }

    /// <summary>Nåværende status (EXPIRING_SOON eller EXPIRED).</summary>
    public CompetencyStatus Status { get; set; }

    /// <summary>ID til brukeren.</summary>
    public Guid UserId { get; set; }

    /// <summary>Brukernavn.</summary>
    public string? UserName { get; set; }

    /// <summary>Brukerens fornavn.</summary>
    public string? UserFirstName { get; set; }

    /// <summary>Brukerens etternavn.</summary>
    public string? UserLastName { get; set; }

    /// <summary>Utløpsdato.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Antall dager til utløp (negativ hvis allerede utløpt).</summary>
    public int? DaysUntilExpiry { get; set; }

    /// <summary>ID til brukerens avdeling.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Navn på brukerens avdeling.</summary>
    public string? DepartmentName { get; set; }
}
