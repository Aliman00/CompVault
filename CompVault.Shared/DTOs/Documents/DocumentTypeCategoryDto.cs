namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// DTO for en dokumenttypekategori.
/// </summary>
public sealed class DocumentTypeCategoryDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>ID til dokumenttypen.</summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>Visningsnavn.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vennlig slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Om kategorien er aktiv.</summary>
    public bool IsActive { get; set; }
}