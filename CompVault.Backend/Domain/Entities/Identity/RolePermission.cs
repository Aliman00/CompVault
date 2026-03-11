namespace CompVault.Backend.Domain.Entities.Identity;

/// <summary>
/// Koblingstabell mellom roller og tillatelser — hvem har lov til hva.
/// </summary>
public class RolePermission
{
    // ======================== Compound Primary Key ========================
    /// <summary>ID til rollen.</summary>
    public Guid RoleId { get; set; }

    /// <summary>ID til tillatelsen.</summary>
    public Guid PermissionId { get; set; }

    // ======================== Historikk ========================
    /// <summary>Når tillatelsen ble gitt (UTC).</summary>
    public DateTime GrantedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Brukeren som ga brukeren tillattelsen</summary>
    public Guid? GrantedById { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    public ApplicationRole Role { get; set; } = null!;

    public ApplicationUser? GrantedBy { get; set; } = null!;
    public Permission Permission { get; set; } = null!;
}
