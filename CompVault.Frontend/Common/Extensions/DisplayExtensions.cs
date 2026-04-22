using MudBlazor;
namespace CompVault.Frontend.Common.Extensions;

public static class DisplayExtensions
{
    /// <summary>
    /// Gjør om et felt til - hvis feltet er tomt
    /// </summary>
    /// <param name="value">En string verdi</param>
    /// <returns>String-verdien eller -</returns>
    public static string DashIfEmpty(this string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value;
    
    /// <summary>
    /// Formatterer filstørrelse fra bytes til et leslig format
    /// </summary>
    /// <param name="bytes">Størrelse i bytes</param>
    /// <returns>Antall bytes i enten B, KB, MB eller GB. Eks: 2.4 MB</returns>
    public static string FormatFileSize(this long? bytes) => bytes switch
    {
        null        => "Ukjent størrelse",
        < 1024      => $"{bytes} B",
        < 1048576   => $"{bytes / 1024.0:F1} KB",
        < 1073741824=> $"{bytes / 1048576.0:F1} MB",
        _           => $"{bytes / 1073741824.0:F1} GB"
    };
    
    /// <summary>
    /// Viser et spesifikt ikon for en fil utifra mimetypen til filen
    /// </summary>
    /// <param name="mimeType">MimeType</param>
    /// <returns>MudBlazor Icon som passer</returns>
    public static string GetFileIcon(this string? mimeType) => mimeType switch
    {
        "application/pdf"                                                    => Icons.Custom.FileFormats.FilePdf,
        "image/png" or "image/jpeg" or "image/gif" or "image/webp"          => Icons.Material.Filled.Image,
        "application/msword" or 
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document" 
            => Icons.Custom.FileFormats.FileWord,
        "application/vnd.ms-excel" or
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"  => Icons.Custom.FileFormats.FileExcel,
        _                                                                     => Icons.Material.Filled.InsertDriveFile
    };

}