using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Opprett en ny kategori under en dokumenttype.
/// </summary>
public sealed class CreateDocumentTypeCategoryRequest
{
    /// <summary>Visningsnavn, f.eks. "Nødsprosedyrer".</summary>
    [Required(ErrorMessage = DocCategoryValidations.Errors.NameRequired)]
    [MaxLength(DocCategoryValidations.NameMaxLength, ErrorMessage = DocCategoryValidations.Errors.NameMaxLength)]
    public string Name { get; set; } = string.Empty;
}