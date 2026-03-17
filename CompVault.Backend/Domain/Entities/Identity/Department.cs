using System.ComponentModel.DataAnnotations;

namespace CompVault.Backend.Domain.Entities.Identity;

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
    [StringLength(250)]
    public string Description { get; set; } = string.Empty;

    /// <summary>ID til overordnet avdeling, hvis den har en. Null = toppnivå.</summary>
    public Guid? ParentDepartmentId { get; set; }

    // ======================== Historikk ========================

    /// <summary>Når avdelingen ble opprettet (UTC).</summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>Brukeren som opprettet avdelingen.</summary>
    public Guid? CreatedById { get; set; }




    // ======================== Navigasjonsegenskaper ========================
    public Department? ParentDepartment { get; set; }
    public ApplicationUser? CreatedBy { get; set; }
    public ICollection<Department> SubDepartments { get; set; } = new List<Department>();
    public ICollection<ApplicationUser> Members { get; set; } = new List<ApplicationUser>();
}
