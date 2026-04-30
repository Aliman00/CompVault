using CompVault.Frontend.Common.Models;
using CompVault.Shared.Result;

using Microsoft.AspNetCore.Components.Forms;

namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Extensionsmetoder mot IBrowserFile-objektet og generilt fil-objekter som FileAttachment
/// </summary>
public static class FileExtensions
{
    /// <summary>
    /// Oppretter en MemoryStream for en fil og bygger en FileAttachment-record til å sendes mellom lag.
    /// Vi gir maksstørrelsen 10% slingringsmoment slik at det er backend som gir feilmelding og ikke frontend
    /// </summary>
    /// <param name="file">IBrowserFile lagt til av bruker</param>
    /// <param name="maxSizeInBytes">Maks størrelse til et dokument</param>
    /// <param name="ct"></param>
    /// <returns>FileAttachment med stream, filnavn og contenttype</returns>
    public static async Task<FileAttachment> ToFileAttachmentAsync(this IBrowserFile file, long maxSizeInBytes, 
        CancellationToken ct = default)
    {
        var buffer = new MemoryStream();
        await using Stream browserStream = file.OpenReadStream(maxAllowedSize: (long)(maxSizeInBytes * 1.1));
        await browserStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        return new FileAttachment(buffer, file.Name, file.ContentType);
    }
    
    /// <summary>
    /// Gjør om en FileAttachment sin stream til en base64-streng
    /// </summary>
    /// <param name="file">FileAttachment</param>
    /// <param name="ct"></param>
    /// <returns>En base64-streng for nedlastning</returns>
    internal static async Task<string> ToBase64Async(this FileAttachment file, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await file.Stream.CopyToAsync(ms, ct);
        return Convert.ToBase64String(ms.ToArray());
    }
    
    /// <summary>
    /// Returnerer true hvis MIME-typen kan vises i nettleseren uten nedlasting. Eks: bilder, pdf
    /// </summary>
    public static bool CanPreviewInBrowser(this string? mimeType) =>
        mimeType is "application/pdf"
            or "image/jpeg"
            or "image/png"
            or "image/gif"
            or "image/webp";
    
    /// <summary>
    /// Speiler backend sin validnering av filer i frontend. Sjekker at den ikke er tom og at filtypen er tillatt
    /// </summary>
    /// <param name="file">Filen vi validerer</param>
    /// <param name="allowedMimeTypes">Tilatte mimetypes</param>
    /// <returns>En feilmeldingsstring eller ingenting</returns>
    public static Result ValidateFile(this IBrowserFile file, IEnumerable<string> allowedMimeTypes)
    {
        if (file.Size == 0)
            return Result.Failure(AppError.Create(ErrorCode.Validation, "Filen er tom."));

        var allowed = allowedMimeTypes.ToList();
        if (allowed.Count > 0 && !allowed.Contains(file.ContentType))
            return Result.Failure(AppError.Create(ErrorCode.Validation,
                $"Filtypen '{file.ContentType}' er ikke tillatt for denne dokumenttypen."));

        return Result.Success();
    }
}