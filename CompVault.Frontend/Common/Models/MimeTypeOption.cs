namespace CompVault.Frontend.Common.Constants;

/// <summary>
/// En MimeType med label og selve typen som string. Eks: "PDF","application/pdf"
/// </summary>
/// <param name="Label">Label er det brukeren ser</param>
/// <param name="MimeType">Det som blir sendt til backend</param>
public record MimeTypeOption(string Label, string MimeType);