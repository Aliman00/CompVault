using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Shared.DTOs.Users;

namespace CompVault.Backend.Features.Users;

public static class UserMapper
{
    public static UserDto ToDto(ApplicationUser user, IList<string> roles) => new()
    {
        Id = user.Id,
        Email = user.Email ?? string.Empty,
        FirstName = user.FirstName,
        LastName = user.LastName,
        JobTitleId = user.JobTitleId,
        JobTitleName = user.JobTitle?.Name,
        JobTitleIsActive = user.JobTitle?.IsActive,
        EmploymentType = user.EmploymentType,
        IsActive = user.IsActive,
        DepartmentId = user.DepartmentId,
        DepartmentName = user.Department?.Name ?? string.Empty,
        DepartmentIsActive = user.Department?.IsActive,
        ManagerId = user.ManagerId,
        ManagerName = user.Manager != null
            ? $"{user.Manager.FirstName} {user.Manager.LastName}".Trim()
            : null,
        CreatedAt = user.CreatedAt,
        Roles = roles.ToList()
    };
    
    public static UserLookupDto ToLookupDto(this ApplicationUser user) => new()
    {
        Id = user.Id,
        FullName = $"{user.FirstName} {user.LastName}",
        JobTitleName = user.JobTitle?.Name,
        DepartmentName = user.Department?.Name,
    };
}