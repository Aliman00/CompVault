using CompVault.Backend.Domain.Entities.Departments;

namespace CompVault.Backend.Infrastructure.Repositories.Departments;

/// <summary>
/// Repository for avdelinger med spørringer for hierarkisk struktur.
/// </summary>
public interface IDepartmentRepository : IRepository<Department>
{
    /// <summary>Henter én avdeling med overordnet og underavdelinger.</summary>
    Task<Department?> GetByIdWithHierarchyAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter alle aktive avdelinger med full hierarki-info.</summary>
    Task<IReadOnlyList<Department>> GetAllWithHierarchyAsync(CancellationToken cancellationToken = default);

    /// <summary>Sjekker om avdelingen har underavdelinger.</summary>
    Task<bool> HasSubDepartmentsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Sjekker om avdelingen har medlemmer.</summary>
    Task<bool> HasMembersAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Henter alle ancestor-IDer til en avdeling (for sirkulær validering).</summary>
    Task<IReadOnlyList<Guid>> GetAncestorIdsAsync(Guid id, CancellationToken cancellationToken = default);

    /// <summary>Soft-sletter avdelingen ved å sette DeletedAt og IsActive.</summary>
    Task SoftDeleteAsync(Department department);
}