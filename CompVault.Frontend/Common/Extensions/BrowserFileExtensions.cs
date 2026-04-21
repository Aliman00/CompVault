using CompVault.Frontend.Common.Models;

using Microsoft.AspNetCore.Components.Forms;

namespace CompVault.Frontend.Common.Extensions;

/// <summary>
/// Extensionsmetoder mot IBrowserFile-objektet
/// </summary>
public static class BrowserFileExtensions
{
    /// <summary>
    /// Oppretter en MemoryStream for en fil og bygger en FileAttachment-record til å sendes mellom lag
    /// </summary>
    /// <param name="file">IBrowserFile lagt til av bruker</param>
    /// <param name="maxSizeInBytes">Maks størrelse til et dokument</param>
    /// <param name="ct"></param>
    /// <returns>FileAttachment med stream, filnavn og contenttype</returns>
    public static async Task<FileAttachment> ToFileAttachmentAsync(this IBrowserFile file, long maxSizeInBytes, 
        CancellationToken ct = default)
    {
        var buffer = new MemoryStream();
        await using Stream browserStream = file.OpenReadStream(maxAllowedSize: maxSizeInBytes);
        await browserStream.CopyToAsync(buffer, ct);
        buffer.Position = 0;

        return new FileAttachment(buffer, file.Name, file.ContentType);
    }
}