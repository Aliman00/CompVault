using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Opprett en ny kategori under en dokumenttype.
/// </summary>
public sealed class CreateDocumentTypeCategoryRequest
{
    /// <summary>Visningsnavn, f.eks. "Nødsprosedyrer".</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vennlig slug, f.eks. "emergency-procedure".</summary>
    [Required]
    [MaxLength(50)]
    public string Slug { get; set; } = string.Empty;
}