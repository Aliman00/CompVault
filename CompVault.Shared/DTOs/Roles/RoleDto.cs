namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// Det klienten ser når de spør etter en rolle.
/// </summary>
public sealed class RoleDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Rollens navn, f.eks. "Admin" eller "Avdelingsleder".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beskrivelse av hva rollen innebærer.</summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>Antall brukere som har denne rollen.</summary>
    public int UserCount { get; set; }

    /// <summary>Når rollen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Brukeren som opprettet rollen.</summary>
    public Guid? CreatedById { get; set; }

    /// <summary>Lista over permissions rollen har.</summary>
    public IReadOnlyList<string> Permissions { get; set; } = new List<string>();
}