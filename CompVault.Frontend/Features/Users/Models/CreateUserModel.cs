using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Enums;

namespace CompVault.Frontend.Features.Users.Models;

public class CreateUserModel
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

    public Guid? JobTitleId { get; set; }

    public EmploymentType EmploymentType { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public List<string> Roles { get; set; } = [];

    public CreateUserRequest ToRequest() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        JobTitleId = JobTitleId,
        EmploymentType = EmploymentType,
        DepartmentId = DepartmentId,
        ManagerId = ManagerId,
        Roles = Roles
    };
}