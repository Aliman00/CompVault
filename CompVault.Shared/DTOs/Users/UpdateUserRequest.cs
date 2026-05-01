using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.Enums;

namespace CompVault.Shared.DTOs.Users;

/// <summary>
/// Felt som kan oppdateres på en bruker. Bare felter som er satt (ikke null) blir endret.
/// </summary>
public sealed class UpdateUserRequest
{
    /// <summary>Nytt fornavn (valgfritt).</summary>
    [MaxLength(UserValidations.FirstNameMaxLength, ErrorMessage = UserValidations.Errors.FirstNameMaxLength)]
    public string? FirstName { get; set; }

    /// <summary>Nytt etternavn (valgfritt).</summary>
    [MaxLength(UserValidations.LastNameMaxLength, ErrorMessage = UserValidations.Errors.LastNameMaxLength)]
    public string? LastName { get; set; }

    /// <summary>Bytt epost (valgfritt).</summary>
    [EmailAddress(ErrorMessage = UserValidations.Errors.EmailInvalid)]
    [MaxLength(UserValidations.EmailMaxLength, ErrorMessage = UserValidations.Errors.EmailMaxLength)]
    public string? Email { get; init => field = value?.Trim(); } = null!;

    /// <summary>Ny stillingstittel-ID (valgfritt).</summary>
    public Guid? JobTitleId { get; set; }

    /// <summary>Sett til true for å fjerne stillingstittel.</summary>
    public bool ClearJobTitleId { get; set; }

    /// <summary>Ny ansettelsestype (valgfritt).</summary>
    public EmploymentType? EmploymentType { get; set; }

    /// <summary>Flytt brukeren til en annen avdeling (valgfritt).</summary>
    public Guid? DepartmentId { get; set; }

    /// <summary>Bytt leder (valgfritt).</summary>
    public Guid? ManagerId { get; set; }

    /// <summary>Sett til true for å fjerne ledertilknytning.</summary>
    public bool ClearManagerId { get; set; }

    /// <summary>Roller brukeren skal ha (overskriver eksisterende, valgfritt).</summary>
    public IReadOnlyList<string>? Roles { get; set; }
}