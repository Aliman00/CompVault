using System.ComponentModel.DataAnnotations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Documents;
using CompVault.Shared.Enums;
namespace CompVault.Frontend.Features.Documents.Models;

public class CreateDocumentTypeModel
{
    [Required(ErrorMessage = DocTypeValidations.Errors.NameRequired)]
    [MaxLength(DocTypeValidations.NameMaxLength, ErrorMessage = DocTypeValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = DocTypeValidations.Errors.SlugRequired)]
    [MaxLength(DocTypeValidations.SlugMaxLength, ErrorMessage = DocTypeValidations.Errors.SlugMaxLength)]
    public string Slug { get; set; } = string.Empty;

    [MaxLength(DocTypeValidations.DescMaxLength, ErrorMessage = DocTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    [Required(ErrorMessage = DocTypeValidations.Errors.TargetModeRequired)]
    public DocumentTargetMode TargetMode { get; set; } = DocumentTargetMode.None;

    public CreateDocumentTypeRequest ToRequest() => new()
    {
        Name        = Name,
        Slug        = Slug,
        Description = Description,
        TargetMode  = TargetMode,
    };
}