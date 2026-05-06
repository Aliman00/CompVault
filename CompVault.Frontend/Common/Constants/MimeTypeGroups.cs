using CompVault.Frontend.Common.Models;
namespace CompVault.Frontend.Common.Constants;

/// <summary>
/// MimeTypes gruppert for å simplifisere valg for brukeren. Alt er på norsk
/// </summary>
public static class MimeTypeGroups
{
    public static readonly List<MimeTypeGroup> All =
    [
        new("Dokumenter", [
            new("PDF","application/pdf"),
            new("Word (.docx)",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document"),
            new("Word (.doc)",
                "application/msword"),
            new("Excel (.xlsx)",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"),
            new("Excel (.xls)",
                "application/vnd.ms-excel"),
            new("PowerPoint (.pptx)",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"),
        ]),
        new("Bilder", [
            new("JPEG","image/jpeg"),
            new("PNG","image/png"),
            new("WebP","image/webp"),
            new("GIF","image/gif"),
        ]),
        new("Tekst", [
            new("Ren tekst (.txt)","text/plain"),
            new("CSV","text/csv"),
        ]),
    ];

    /// <summary>
    /// Henter ut alle mimetypes i en liste for å enklere slå opp hva en label er
    /// </summary>
    public static readonly IReadOnlyDictionary<string, string> LabelByMimeType =
        All.SelectMany(g => g.Types).ToDictionary(t => t.MimeType, t => t.Label);
}