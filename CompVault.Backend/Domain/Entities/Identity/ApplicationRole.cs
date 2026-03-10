using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace CompVault.Backend.Domain.Entities.Identity;

/// <summary>
/// En rolle i systemet, f.eks. "Admin" eller "Manager".
/// Bygger på Identity sin innebygde rolleklasse.
/// </summary>
public class ApplicationRole : IdentityRole<Guid>
{
    // IdentityRole egenskaper:
    // - Id (Guid)
    // - Name (string?)
    // - NormalizedName (string?)
    // - ConcurrencyStamp (string?)
    
    // ======================== Rolle egenskaper ========================
    /// <summary>Kort forklaring av hva rollen innebærer</summary>
    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Når rollen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    
    /// <summary>Brukeren som opprettet rollen</summary>
    public Guid? CreatedById { get; set; }
    
    // ======================== Navigasjonsegenskaper ========================
    public ApplicationUser? CreatedBy { get; set; }
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
