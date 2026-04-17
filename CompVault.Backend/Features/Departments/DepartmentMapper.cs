using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Shared.DTOs.Departments;

namespace CompVault.Backend.Features.Departments;

/// <summary>
/// Mapper for konvertering mellom <see cref="Department"/> og <see cref="DepartmentDto"/>.
/// </summary>
public static class DepartmentMapper
{
    /// <summary>
    /// Konverterer en <see cref="Department"/> til en <see cref="DepartmentDto"/>.
    /// </summary>
    public static DepartmentDto ToDto(Department department, int subDepartmentCount) => new()
    {
        Id = department.Id,
        Name = department.Name,
        Description = department.Description,
        ParentDepartmentId = department.ParentDepartmentId,
        ParentDepartmentName = department.ParentDepartment?.Name,
        SubDepartmentCount = subDepartmentCount,
        IsActive = department.IsActive,
        CreatedAt = department.CreatedAt,
        CreatedById = department.CreatedById,
        CreatedByName = department.CreatedBy != null 
            ? $"{department.CreatedBy.FirstName} {department.CreatedBy.LastName}" 
            : null
    };
}