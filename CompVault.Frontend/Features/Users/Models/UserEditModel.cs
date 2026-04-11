using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Enums;
namespace CompVault.Frontend.Features.Users.Models;


/// <summary>
/// Modellen for å endre en bruker
/// </summary>
public class UserEditModel
{
    [Required(ErrorMessage = UserValidations.Errors.FirstNameRequired)]
    [MaxLength(UserValidations.FirstNameMaxLength, ErrorMessage = UserValidations.Errors.FirstNameMaxLength)]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = UserValidations.Errors.LastNameRequired)]
    [MaxLength(UserValidations.LastNameMaxLength, ErrorMessage = UserValidations.Errors.LastNameMaxLength)]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = UserValidations.Errors.EmailRequired)]
    [EmailAddress(ErrorMessage = UserValidations.Errors.EmailInvalid)]
    [MaxLength(UserValidations.EmailMaxLength, ErrorMessage = UserValidations.Errors.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    [MaxLength(UserValidations.JobTitleMaxLength, ErrorMessage = UserValidations.Errors.JobTitleMaxLength)]
    public string JobTitle { get; set; } = string.Empty;
    
    public EmploymentType EmploymentType { get; set; }
    public bool IsActive { get; set; }
    
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
        ManagerId = dto.ManagerId,
        DepartmentId = dto.DepartmentId,
    };
    
    public UpdateUserRequest ToRequest() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        JobTitle = JobTitle,
        EmploymentType = EmploymentType,
        DepartmentId = DepartmentId,
        ClearDepartmentId = DepartmentId == null,
        ManagerId = ManagerId,
        ClearManagerId = ManagerId == null
    };
}