using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Opprett en ny kategori under en dokumenttype.
/// </summary>
public sealed class CreateDocumentTypeCategoryRequest
{
    /// <summary>Visningsnavn, f.eks. "Nødsprosedyrer". Genererer automatisk URL-vennlig slug.</summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;
}