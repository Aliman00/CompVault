namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Henter og viser en DTO for en bruker til dokument oversikt-siden
/// </summary>
public class UserDocumentTypeDto
{
    /// <summary> ID-en til en dokumentkategori/type </summary>
    public Guid Id { get; set; }
    
    /// <summary> Navnet til dokumentkategorien/typen </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary> Slug-en til dokumentkategorien/typen </summary>
    public string Slug { get; set; } = string.Empty;
    
    /// <summary> Beskrivelsen til dokumentkategorien/typen </summary>
    public string? Description { get; set; }
    
    /// <summary> Antall dokumenter brukeren har til hver type</summary>
    public int DocumentCount { get; set; }
    
    /// <summary> Antall dokumenter til hver type bruker er nødt til å signere</summary>
    public int PendingSignatureCount { get; set; }
}