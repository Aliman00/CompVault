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
        JobTitle = user.JobTitle,
        EmploymentType = user.EmploymentType,
        IsActive = user.IsActive,
        DepartmentId = user.DepartmentId,
        ManagerId = user.ManagerId,
        CreatedAt = user.CreatedAt,
        Roles = roles.ToList()
    };
}
