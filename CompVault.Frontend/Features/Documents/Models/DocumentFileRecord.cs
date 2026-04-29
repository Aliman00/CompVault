namespace CompVault.Frontend.Features.Documents.Models;

/// <summary>
/// En samling av informasjon en fil trenger for å kunne vise alle feltene i DocumentFileCard
/// </summary>
/// <param name="DocumentId">ID-for å laste ned fil</param>
/// <param name="Slug">Slug for å laste ned filen</param>
/// <param name="MimeType">MimeType bestemmer ikonet</param>
/// <param name="FileName">Viser navnet på filen</param>
/// <param name="FileSize">Størrelse i KB, MB eller Gb</param>
/// <param name="Version">Versjonen av filen</param>
/// <param name="UploadedBy">Hvem som har lastet opp ID</param>
/// <param name="UploadedByName">Hvem som har lastet opp Navn</param>
/// <param name="UploadedAt">Når den er lastet opp</param>
public record DocumentFileRecord(
    Guid DocumentId,
    string Slug,
    string MimeType,
    string FileName,
    long? FileSize,
    int Version,
    Guid UploadedBy,
    string UploadedByName,
    DateTime UploadedAt);