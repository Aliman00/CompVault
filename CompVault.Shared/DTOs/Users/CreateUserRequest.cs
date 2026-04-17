using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Users;

/// <summary>
/// Det som sendes inn for å opprette en ny bruker.
/// </summary>
public sealed class CreateUserRequest
{
    /// <summary>E-postadressen (brukes som brukernavn).</summary>
    [Required(ErrorMessage = UserValidations.Errors.EmailRequired)]
    [EmailAddress(ErrorMessage = UserValidations.Errors.EmailInvalid)]
    [MaxLength(UserValidations.EmailMaxLength, ErrorMessage = UserValidations.Errors.EmailMaxLength)]
    public string Email { get; set; } = string.Empty;

    /// <summary>Fornavn.</summary>
    [Required(ErrorMessage = UserValidations.Errors.FirstNameRequired)]
    [MaxLength(UserValidations.FirstNameMaxLength, ErrorMessage = UserValidations.Errors.FirstNameMaxLength)]
    public string FirstName { get; set; } = string.Empty;

    /// <summary>Etternavn.</summary>
    [Required(ErrorMessage = UserValidations.Errors.LastNameRequired)]
    [MaxLength(UserValidations.LastNameMaxLength, ErrorMessage = UserValidations.Errors.LastNameMaxLength)]
    public string LastName { get; set; } = string.Empty;

    /// <summary>ID til stillingstittelen (valgfritt).</summary>
    public Guid? JobTitleId { get; set; }

    /// <summary>Ansettelsestype.</summary>
    [Required]
    public EmploymentType EmploymentType { get; set; } = EmploymentType.Permanent;

    /// <summary>Valgfri avdelings-ID.</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Valgfri leder-ID.</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Rollene som skal tildeles brukeren med en gang.</summary>
    public IList<string> Roles { get; set; } = [];
}