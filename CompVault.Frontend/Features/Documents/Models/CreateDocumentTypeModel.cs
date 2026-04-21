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

    [MaxLength(DocTypeValidations.DescMaxLength, ErrorMessage = DocTypeValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    [Required(ErrorMessage = DocTypeValidations.Errors.TargetModeRequired)]
    public DocumentTargetMode TargetMode { get; set; } = DocumentTargetMode.None;
    
    public string[] AllowedMimeTypes { get; set; } = ["application/pdf"];

    [Range(DocTypeValidations.MaxFileSizeMinMb, DocTypeValidations.MaxFileSizeMaxMb,
        ErrorMessage = DocTypeValidations.Errors.MaxFileSizeRange)]
    public long MaxFileSizeMb { get; set; } = 20;

    public long MaxFileSizeBytes => MaxFileSizeMb * 1024 * 1024;

    public CreateDocumentTypeRequest ToRequest() => new()
    {
        Name = Name,
        Description = Description,
        TargetMode = TargetMode,
        AllowedMimeTypes = AllowedMimeTypes,
        MaxFileSizeBytes = MaxFileSizeBytes,
    };
}