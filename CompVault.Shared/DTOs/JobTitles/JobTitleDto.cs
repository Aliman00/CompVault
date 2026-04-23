namespace CompVault.Shared.DTOs.JobTitles;

/// <summary>
/// DTO for en stillingstittel.
/// </summary>
public sealed class JobTitleDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Navn på stillingstittelen.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Om stillings med denne tittelen regnes som ledere.</summary>
    public bool IsLeader { get; set; }

    /// <summary>Om stillingstittelen er aktiv.</summary>
    public bool IsActive { get; set; }
}