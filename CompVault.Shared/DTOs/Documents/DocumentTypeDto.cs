using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// DTO for en dokumenttype.
/// </summary>
public sealed class DocumentTypeDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Visningsnavn.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>URL-vennlig slug.</summary>
    public string Slug { get; set; } = string.Empty;

    /// <summary>Beskrivelse.</summary>
    public string? Description { get; set; }

    /// <summary>Targeting-modus.</summary>
    public DocumentTargetMode TargetMode { get; set; }

    /// <summary>Tillatte MIME-typer for opplasting.</summary>
    public string[] AllowedMimeTypes { get; set; } = [];

    /// <summary>Maksimal filstørrelse i bytes.</summary>
    public long MaxFileSizeBytes { get; set; }

    /// <summary>Om dokumenttypen er aktiv.</summary>
    public bool IsActive { get; set; }

    /// <summary>Når dokumenttypen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Antall kategorier.</summary>
    public int CategoryCount { get; set; }
}