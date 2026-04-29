using System.ComponentModel.DataAnnotations;

using CompVault.Shared.Constants.Validations;

namespace CompVault.Shared.DTOs.Documents;

/// <summary>
/// Opprett et nytt dokument.
/// </summary>
public sealed class CreateDocumentRequest
{
    /// <summary>Dokumenttittel.</summary>
    [Required(ErrorMessage = DocValidations.Errors.TitleRequired)]
    [MaxLength(DocValidations.TitleMaxLength, ErrorMessage = DocValidations.Errors.TitleMaxLength)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Beskrivelse av dokumentet.</summary>
    [MaxLength(DocValidations.DescMaxLength, ErrorMessage = DocValidations.Errors.DescMaxLength)]
    public string? Description { get; set; }

    /// <summary>ID til kategorien (DocumentTypeCategory). Null = ukategorisert.</summary>
    public Guid? DocumentTypeCategoryId { get; set; }

    /// <summary>Ekstern URL i stedet for filopplasting.</summary>
    [MaxLength(DocValidations.ExternalUrlMaxLength, ErrorMessage = DocValidations.Errors.ExternalUrlMaxLength)]
    [Url(ErrorMessage = DocValidations.Errors.ExternalUrlFormat)]
    public string? ExternalUrl { get; set; }

    /// <summary>ID-er til målavdelinger. Brukes når DocumentType.TargetMode er Department.</summary>
    public List<Guid> TargetDepartmentIds { get; set; } = [];

    /// <summary>ID-er til mål-jobbtitler. Brukes når DocumentType.TargetMode er JobTitle.</summary>
    public List<Guid> TargetJobTitleIds { get; set; } = [];

    /// <summary>Om dokumentet krever signering. Standard er true.</summary>
    public bool RequiresSignature { get; set; } = true;
}