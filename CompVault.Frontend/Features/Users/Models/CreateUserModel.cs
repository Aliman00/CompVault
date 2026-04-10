using System.ComponentModel.DataAnnotations;

using CompVault.Shared.DTOs.Users;
using CompVault.Shared.Enums;

namespace CompVault.Frontend.Features.Users.Models;

public class CreateUserModel
{
    [Required(ErrorMessage = "Fornavn er påkrevd")]
    [MaxLength(100, ErrorMessage = "Fornavn kan ikke være mer enn 100 tegn")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Etternavn er påkrevd")]
    [MaxLength(100, ErrorMessage = "Etternavn kan ikke være mer enn 100 tegn")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "E-post er påkrevd")]
    [EmailAddress(ErrorMessage = "Ugyldig e-postadresse")]
    [MaxLength(256, ErrorMessage = "E-post kan ikke være mer enn 256 tegn")]
    public string Email { get; set; } = string.Empty;

    [MaxLength(150, ErrorMessage = "Job tittel kan ikke være mer enn 150 tegn")]
    public string JobTitle { get; set; } = string.Empty;

    public EmploymentType EmploymentType { get; set; }
    public Guid? DepartmentId { get; set; }
    public Guid? ManagerId { get; set; }
    public List<string> Roles { get; set; } = [];

    public CreateUserRequest ToRequest() => new()
    {
        FirstName = FirstName,
        LastName = LastName,
        Email = Email,
        JobTitle = JobTitle,
        EmploymentType = EmploymentType,
        DepartmentId = DepartmentId,
        ManagerId = ManagerId,
        Roles = Roles
    };
}