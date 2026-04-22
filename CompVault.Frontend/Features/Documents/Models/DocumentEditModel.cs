using System.ComponentModel.DataAnnotations;

using CompVault.Frontend.Common.Validations;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Documents;
namespace CompVault.Frontend.Features.Documents.Models;

public class DocumentEditModel
{
    [Required(ErrorMessage = DocValidations.Errors.TitleRequired)]
    [MaxLength(DocValidations.TitleMaxLength, ErrorMessage = DocValidations.Errors.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(DocValidations.DescMaxLength, ErrorMessage = DocValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }
    
    public Guid? DocumentTypeCategoryId { get; set; }

    [MaxLength(DocValidations.ExternalUrlMaxLength, ErrorMessage = DocValidations.Errors.ExternalUrlMaxLength)]
    [OptionalUrl(ErrorMessage = DocValidations.Errors.ExternalUrlFormat)]
    public string? ExternalUrl { get; set; }

    public bool RequiresSignature { get; set; } = true;

    public List<Guid> TargetDepartmentIds { get; set; } = [];
    public List<Guid> TargetJobTitleIds { get; set; } = [];

    public static DocumentEditModel FromDto(DocumentDto dto) => new()
    {
        Title = dto.Title,
        Description = dto.Description,
        DocumentTypeCategoryId = dto.DocumentTypeCategoryId,
        ExternalUrl = dto.ExternalUrl,
        RequiresSignature = dto.RequiresSignature,
        TargetDepartmentIds = dto.TargetDepartmentIds,
        TargetJobTitleIds = dto.TargetJobTitleIds,
    };

    public UpdateDocumentRequest ToRequest() => new()
    {
        Title = Title,
        Description = Description,
        DocumentTypeCategoryId = DocumentTypeCategoryId,
        ClearDocumentTypeCategoryId = DocumentTypeCategoryId is null,
        ExternalUrl = string.IsNullOrWhiteSpace(ExternalUrl) ? null : ExternalUrl,
        ClearExternalUrl = string.IsNullOrWhiteSpace(ExternalUrl),
        RequiresSignature = RequiresSignature,
        TargetDepartmentIds = TargetDepartmentIds,
        TargetJobTitleIds = TargetJobTitleIds,
    };
}