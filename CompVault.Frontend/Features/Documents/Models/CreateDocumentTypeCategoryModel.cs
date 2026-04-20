using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Documents;
namespace CompVault.Frontend.Features.Documents.Models;

public class CreateDocumentTypeCategoryModel
{
    [Required(ErrorMessage = DocCategoryValidations.Errors.NameRequired)]
    [MaxLength(DocCategoryValidations.NameMaxLength, ErrorMessage = DocCategoryValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;

    public CreateDocumentTypeCategoryRequest ToRequest() => new()
    {
        Name = Name,
    };
}