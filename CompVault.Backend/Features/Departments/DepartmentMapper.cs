using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.DTOs.Departments;

namespace CompVault.Backend.Features.Departments;

/// <summary>
/// Mapper for konvertering mellom <see cref="Department"/> og <see cref="DepartmentDto"/>.
/// </summary>
public static class DepartmentMapper
{
    /// <summary>
    /// Konverterer en <see cref="Department"/> til en <see cref="DepartmentDto"/>, inkludert antall underavdelinger og medlemmer.
    /// </summary>
    /// <returns>En <see cref="DepartmentDto"/> som representerer avdelingen.</returns>
    public static DepartmentDto ToDto(Department department, int subDepartmentCount, int memberCount) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
        ParentDepartmentId = department.ParentDepartmentId,
        ParentDepartmentName = department.ParentDepartment?.Name,
        SubDepartmentCount = subDepartmentCount,
        MemberCount = memberCount,
        IsActive = department.IsActive,
        CreatedAt = department.CreatedAt,
        CreatedById = department.CreatedById
    };
}
