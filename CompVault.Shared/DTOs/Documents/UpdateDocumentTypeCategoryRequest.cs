using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Oppdater en eksisterende kategori under en dokumenttype.
/// </summary>
public sealed class UpdateDocumentTypeCategoryRequest
{
    /// <summary>Nytt visningsnavn.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Ny URL-vennlig slug.</summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;
}