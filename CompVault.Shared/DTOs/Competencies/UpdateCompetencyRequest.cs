using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Competencies;

/// <summary>
/// Det som sendes inn for å oppdatere et kompetansebevis. Alle felt er nullable
/// for å støtte partial update.
/// </summary>
public sealed class UpdateCompetencyRequest
{
    /// <summary>Ny utløpsdato.</summary>
    public DateTime? ExpiryDate { get; set; }

    /// <summary>Ny utstedelsesdato.</summary>
    public DateTime? IssuedDate { get; set; }

    /// <summary>Nytt sertifikatnummer.</summary>
    [MaxLength(100)]
    public string? CertificateNumber { get; set; }

    /// <summary>Nye notater.</summary>
    [StringLength(2000)]
    public string? Notes { get; set; }

    /// <summary>
    /// Sett til <see cref="CompetencyStatus.Revoked"/> for å tilbakekalle beviset.
    /// Krever at <see cref="RevokedReason"/> fylles ut.
    /// </summary>
    public CompetencyStatus? Status { get; set; }

    /// <summary>
    /// Årsak til tilbakekalling. Påkrevt hvis <see cref="Status"/> settes til Revoked.
    /// </summary>
    public string? RevokedReason { get; set; }
}
