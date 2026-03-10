using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Domain.Entities.Identity;

/// <summary>
/// En konkret tillatelse som kan tildeles roller, f.eks. "users:read" eller "reports:export".
/// </summary>
public class Permission
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();
    
    // ======================== Permission egenskaper ========================
    /// <summary>Unikt navn på tillatelsen, f.eks. "users:read".</summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>Hva tillatelsen egentlig gir tilgang til.</summary>
    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    /// <summary>Hvilken del av appen tillatelsen hører til, f.eks. "Users" eller "Reports".</summary> 
    public string Category { get; set; } = string.Empty;

    // ======================== Navigasjonsegenskaper ========================
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}
