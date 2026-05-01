using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Departments;
namespace CompVault.Frontend.Features.Departments.Models;


/// <summary>
/// Modellen for å endre en avdeling
/// </summary>
public class DepartmentEditModel
{
    [Required(ErrorMessage = DepValidations.Errors.NameRequired)]
    [MaxLength(DepValidations.NameMaxLength, ErrorMessage = DepValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [MaxLength(DepValidations.DescriptionMaxLength, ErrorMessage = DepValidations.Errors.DescriptionMaxLength)]
    public string? Description { get; set; }

    public Guid? ParentDepartmentId { get; set; }

    public bool ClearParentDepartment { get; set; }

    public bool IsActive { get; set; }

    public Guid? ManagerId { get; set; }

    public static DepartmentEditModel FromDto(DepartmentDto dto) => new()
    {
        Name = dto.Name,
        Description = dto.Description,
        ParentDepartmentId = dto.ParentDepartmentId,
        IsActive = dto.IsActive,
        ManagerId = dto.ManagerId,
    };

    public UpdateDepartmentRequest ToRequest(bool clearParentDepartment, bool clearManagerId = false) => new()
    {
        Name = Name,
        Description = Description,
        ParentDepartmentId = ParentDepartmentId,
        ClearParentDepartment = clearParentDepartment,
        ManagerId = ManagerId,
        ClearManagerId = clearManagerId,
        IsActive = IsActive,
    };
}