using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Documents;

namespace CompVault.Frontend.Features.Documents.Models;

public class DocumentTypeCategoryEditModel
{
    [Required(ErrorMessage = DocCategoryValidations.Errors.NameRequired)]
    [MaxLength(DocCategoryValidations.NameMaxLength, ErrorMessage = DocCategoryValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public static DocumentTypeCategoryEditModel FromDto(DocumentTypeCategoryDto dto) => new()
    {
        Name = dto.Name,
        IsActive = dto.IsActive,
    };

    public UpdateDocumentTypeCategoryRequest ToRequest() => new()
    {
        Name = Name,
        IsActive = IsActive,
    };
}