using System.ComponentModel.DataAnnotations;

using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;

namespace CompVault.Backend.Domain.Entities.Departments;

/// <summary>
/// En avdeling i organisasjonen. Kan ha underavdelinger (hierarkisk struktur).
/// </summary>
public class Department
{
    // ======================== Primary Key ========================
    /// <summary>Unik ID.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    // ======================== Department egenskaper ========================

    /// <summary>Avdelingens navn.</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse av hva avdelingen driver med.</summary>
    [StringLength(500)]
    public string Description { get; set; } = string.Empty;

    /// <summary>ID til overordnet avdeling, hvis den har en. Null = toppnivå.</summary>
    public Guid? ParentDepartmentId { get; set; }

    // ======================== Historikk ========================

    /// <summary>Når avdelingen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Brukeren som opprettet avdelingen.</summary>
    public Guid? CreatedById { get; set; }

    /// <summary>Om avdelingen er aktiv (soft delete via IsActive + DeletedAt).</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Når avdelingen ble soft-slettet (UTC). Null hvis aktiv.</summary>
    public DateTime? DeletedAt { get; set; }

    // ======================== Navigasjonsegenskaper ========================
    public Department? ParentDepartment { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();

    /// <summary>Navigasjonsegenskap for dokumenter rettet mot avdelinger.</summary>
    public ICollection<DocumentDepartment> DocumentDepartments { get; set; } = new List<DocumentDepartment>();
}