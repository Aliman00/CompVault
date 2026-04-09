using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Enums;

namespace CompVault.Frontend.Features.Users.Models;

public class UserEditModel
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string JobTitle { get; set; } = string.Empty;
    public EmploymentType EmploymentType { get; set; }
    public bool IsActive { get; set; }
    
    public DateTime CreatedAt { get; set; }
    
    public string? DepartmentName { get; set; }
    
    public string? ManagerName { get; set; }
    
    public Guid? DepartmentId { get; set; }
    
    public Guid? ManagerId { get; set; }

    public static UserEditModel FromDto(UserDto dto) => new()
    {
        FirstName = dto.FirstName,
        LastName = dto.LastName,
        Email = dto.Email,
        JobTitle = dto.JobTitle,
        EmploymentType = dto.EmploymentType,
        IsActive = dto.IsActive,
        CreatedAt = dto.CreatedAt,
        ManagerId = dto.ManagerId,
        ManagerName = dto.ManagerName,
        DepartmentId = dto.DepartmentId,
        DepartmentName = dto.DepartmentName
    };
}