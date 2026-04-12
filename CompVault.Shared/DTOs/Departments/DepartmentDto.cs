namespace CompVault.Shared.DTOs.Departments;

/// <summary>
/// Det klienten ser når de spør etter en avdeling.
/// </summary>
public sealed class DepartmentDto
{
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; }

    /// <summary>Avdelingens navn.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Beskrivelse av hva avdelingen driver med.</summary>
    public string? Description { get; set; }

    /// <summary>ID til overordnet avdeling (null hvis toppnivå).</summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>Navn på overordnet avdeling (null hvis toppnivå).</summary>
    public string? ParentDepartmentName { get; set; }

    /// <summary>Antall direkte underavdelinger.</summary>
    public int SubDepartmentCount { get; set; }

    /// <summary>Om avdelingen er aktiv.</summary>
    public bool IsActive { get; set; }

    /// <summary>Når avdelingen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>ID til brukeren som opprettet avdelingen.</summary>
    public Guid? CreatedById { get; set; }
    
    /// <summary>Navn på brukeren som opprettet avdelingen.</summary>
    public string? CreatedByName { get; set; }
}