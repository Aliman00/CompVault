using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.JobTitles;

/// <summary>
/// Oppdater en stillingstittel.
/// </summary>
public sealed class UpdateJobTitleRequest
{
    /// <summary>Nytt navn.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}