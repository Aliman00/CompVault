namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// DTO for en permission.
/// </summary>
public sealed class ExpiringCompetencyDto
{
    /// <summary>Unikt navn på tillatelsen, f.eks. "users:read".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Hva tillatelsen egentlig gir tilgang til.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Hvilken del av appen tillatelsen hører til, f.eks. "Users" eller "Reports".</summary>
    public string Category { get; set; } = string.Empty;
}