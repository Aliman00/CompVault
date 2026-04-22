using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Documents;

namespace CompVault.Frontend.Features.Documents.Models;

public class CreateDocumentModel
{
    [Required(ErrorMessage = DocValidations.Errors.TitleRequired)]
    [MaxLength(DocValidations.TitleMaxLength, ErrorMessage = DocValidations.Errors.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    [MaxLength(DocValidations.DescMaxLength, ErrorMessage = DocValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    [MaxLength(DocValidations.ExternalUrlMaxLength, ErrorMessage = DocValidations.Errors.ExternalUrlMaxLength)]
    [Url(ErrorMessage = DocValidations.Errors.ExternalUrlFormat)]
    public string? ExternalUrl { get; set; }

    public bool RequiresSignature { get; set; } = true;
    
    public List<Guid> TargetDepartmentIds { get; set; } = [];
    public List<Guid> TargetJobTitleIds { get; set; } = [];

    public CreateDocumentRequest ToRequest() => new()
    {
        Title = Title,
        Description = Description,
        ExternalUrl = ExternalUrl,
        RequiresSignature = RequiresSignature,
        TargetDepartmentIds = TargetDepartmentIds,
        TargetJobTitleIds = TargetJobTitleIds
    };
}