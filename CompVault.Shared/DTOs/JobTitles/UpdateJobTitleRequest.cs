using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.JobTitles;

/// <summary>
/// Oppdater en stillingstittel.
/// </summary>
public sealed class UpdateJobTitleRequest
{
    /// <summary>Nytt navn.</summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>Om stillingstittelen skal være aktiv. Null = ikke endret.</summary>
    public bool? IsActive { get; set; }
}
