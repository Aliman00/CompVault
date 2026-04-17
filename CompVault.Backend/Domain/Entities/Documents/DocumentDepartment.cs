using CompVault.Backend.Domain.Entities.Departments;

namespace CompVault.Backend.Domain.Entities.Documents;

/// <summary>
/// Koblingstabell mellom dokument og avdeling for målgruppe.
/// Brukes når DocumentType.TargetMode er Department.
/// </summary>
public class DocumentDepartment
{
    /// <summary>ID til dokumentet.</summary>
    public Guid DocumentId { get; set; }

    /// <summary>ID til avdelingen.</summary>
    public Guid DepartmentId { get; set; }

    /// <summary>Navigasjon til dokumentet.</summary>
    public Document? Document { get; set; }

    /// <summary>Navigasjon til avdelingen.</summary>
    public Department? Department { get; set; }
}