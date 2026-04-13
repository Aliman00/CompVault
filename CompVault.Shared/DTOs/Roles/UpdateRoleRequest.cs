using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Roles;

/// <summary>
/// Det som sendes inn for å oppdatere en eksisterende rolle.
/// Alle felt er nullable for partial update.
/// </summary>
public sealed class UpdateRoleRequest
{
    /// <summary>Rollens navn.</summary>
    [MinLength(RoleValidations.NameMinLength, ErrorMessage = RoleValidations.Errors.NameMinLength)]
    [MaxLength(RoleValidations.NameMaxLength, ErrorMessage = RoleValidations.Errors.NameMaxLength)]
    public string? Name { get; set; }

    /// <summary>Beskrivelse av hva rollen innebærer.</summary>
    [MinLength(RoleValidations.DescriptionMinLength, ErrorMessage = RoleValidations.Errors.DescriptionMinLength)]
    [MaxLength(RoleValidations.DescriptionMaxLength, ErrorMessage = RoleValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }
}