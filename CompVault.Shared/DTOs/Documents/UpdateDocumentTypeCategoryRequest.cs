using System.ComponentModel.DataAnnotations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Oppdater en eksisterende kategori under en dokumenttype.
/// Alle felt er nullable for å støtte partial update.
/// Slug genereres automatisk fra navn ved endring.
/// </summary>
public sealed class UpdateDocumentTypeCategoryRequest
{
    /// <summary>Nytt visningsnavn. Null = ikke endret. Genererer automatisk ny slug.</summary>
    [MaxLength(100)]
    public string? Name { get; set; }

    /// <summary>Om kategorien skal være aktiv. Null = ikke endret.</summary>
    public bool? IsActive { get; set; }
}