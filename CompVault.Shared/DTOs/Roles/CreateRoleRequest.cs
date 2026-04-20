using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// Det som sendes inn for å opprette en ny rolle.
/// </summary>
public sealed class CreateRoleRequest
{
    /// <summary>Rollens navn, f.eks. "Avdelingsleder".</summary>
    [Required(ErrorMessage = RoleValidations.Errors.NameRequired)]
    [MinLength(RoleValidations.NameMinLength, ErrorMessage = RoleValidations.Errors.NameMinLength)]
    [MaxLength(RoleValidations.NameMaxLength, ErrorMessage = RoleValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Beskrivelse av hva rollen innebærer.</summary>
    [Required(ErrorMessage = RoleValidations.Errors.DescriptionRequired)]
    [MinLength(RoleValidations.DescriptionMinLength, ErrorMessage = RoleValidations.Errors.DescriptionMinLength)]
    [MaxLength(RoleValidations.DescriptionMaxLength, ErrorMessage = RoleValidations.Errors.DescriptionMaxLength)]
    public string Description { get; set; } = string.Empty;
}