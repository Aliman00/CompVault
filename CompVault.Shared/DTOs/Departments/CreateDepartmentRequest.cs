using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Departments;

/// <summary>
/// Det som sendes inn for å opprette en ny avdeling.
/// </summary>
public sealed class CreateDepartmentRequest
{
    /// <summary>Avdelingens navn.</summary>
    [Required(ErrorMessage = DepValidations.Errors.NameRequired)]
    [MaxLength(DepValidations.NameMaxLength, ErrorMessage = DepValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Valgfri beskrivelse av hva avdelingen driver med.</summary>
    [MaxLength(DepValidations.DescriptionMaxLength, ErrorMessage = DepValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }

    /// <summary>ID til overordnet avdeling (valgfritt — null = toppnivå).</summary>
    public Guid? ParentDepartmentId { get; set; }

    /// <summary>ID til brukeren som skal lede avdelingen.</summary>
    public Guid? ManagerId { get; set; }
}
