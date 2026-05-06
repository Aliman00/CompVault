using CompVault.Shared.DTOs.Common.Pagination;

namespace CompVault.Shared.DTOs.Audit;

/// <summary>
/// Query-parametere for filtrering og paginering av revisjonsloggen.
/// Arver paginering fra <see cref="PagedQuery"/> og legger til audit-spesifikke filtre.
/// </summary>
public record AuditLogQueryParameters : PagedQuery
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
}