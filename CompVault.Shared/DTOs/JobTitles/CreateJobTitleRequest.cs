using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.JobTitles;

/// <summary>
/// Opprett en ny stillingstittel.
/// </summary>
public sealed class CreateJobTitleRequest
{
    /// <summary>Navn på stillingstittelen.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}