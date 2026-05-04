using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Domain.Entities.Audit;

/// <summary>
/// Sentral revisjonslogg som fanger alle vesentlige endringer i systemet.
/// Uavhengig av ApplicationUser sin soft-delete — UserEmail/UserName er
/// denormalisert for at historikken alltid skal være tilgjengelig.
/// </summary>
public class AuditLog
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Handlingstype, f.eks. "competency.revoke", "document.create".
    /// </summary>
    public string Action { get; set; } = string.Empty;

    /// <summary>
    /// Entitetstype som ble endret, f.eks. "Competency", "Document".
    /// </summary>
    public string EntityType { get; set; } = string.Empty;

    /// <summary>ID til entiteten som ble endret.</summary>
    public Guid EntityId { get; set; }

    /// <summary>
    /// ID til brukeren som utførte handlingen. Null for bakgrunnsjobber.
    /// Ingen FK — AuditLog er uavhengig av ApplicationUser.
    /// </summary>
    public Guid? UserId { get; set; }

    /// <summary>Denormalisert e-post for rask visning, selv etter bruker deaktiveres.</summary>
    public string? UserEmail { get; set; }

    /// <summary>Denormalisert navn for rask visning, selv etter bruker deaktiveres.</summary>
    public string? UserName { get; set; }

    /// <summary>
    /// Fleksible detaljer per action-type, lagret som JSONB.
    /// F.eks. changed_fields, revoked_reason, old_version/new_version.
    /// </summary>
    [MaxLength(5000)]
    public string? Details { get; set; }

    /// <summary>Når handlingen ble utført (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}