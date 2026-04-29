using CompVault.Frontend.Features.Documents.Models;
using CompVault.Shared.DTOs.Documents;
namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Extensions for DTO-er for eksempel å mappe til andre objekter/records
/// </summary>
public static class DtoExtensions
{
    /// <summary>
    /// Mapper fra DocumentDto til DocumentFileRecord hvis filen er med
    /// </summary>
    /// <param name="dto">DocumentDto</param>
    /// <returns></returns>
    public static DocumentFileRecord? ToFileInfo(this DocumentDto dto) => dto.HasFile ? new DocumentFileRecord(
        dto.Id,
        dto.DocumentTypeSlug,
        dto.MimeType!,
        dto.FileName!,
        dto.FileSize!.Value,
        dto.Version,
        dto.UploadedBy,
        dto.UploadedByName!,
        dto.UploadedAt) : null;
}