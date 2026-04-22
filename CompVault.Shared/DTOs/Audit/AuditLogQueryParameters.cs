namespace CompVault.Shared.DTOs.Audit;

/// <summary>
/// Query-parametere for filtrering og paginering av revisjonsloggen.
/// </summary>
public class AuditLogQueryParameters
{
    /// <summary>Filtrer på action-type, f.eks. "competency.revoke".</summary>
    public string? Action { get; set; }

    /// <summary>Filtrer på entitet-type, f.eks. "Competency".</summary>
    public string? EntityType { get; set; }

    /// <summary>Filtrer på spesifikk entitet.</summary>
    public Guid? EntityId { get; set; }

    /// <summary>Filtrer på hvem som utførte handlingen.</summary>
    public Guid? UserId { get; set; }

    /// <summary>Fra-dato (inclusive).</summary>
    public DateTime? From { get; set; }

    /// <summary>Til-dato (exclusive).</summary>
    public DateTime? To { get; set; }

    /// <summary>Side (1-basert, default 1).</summary>
    public int Page { get; set; } = 1;

    /// <summary>Antall per side (default 50, max 100).</summary>
    public int PageSize { get; set; } = 50;
}