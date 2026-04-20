using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <summary>
/// Håndterer målgruppe-logikk for dokumenter: validering av target-lister,
/// tilgangssjekk basert på avdeling/jobbtittel, og avdelingshierarki.
/// </summary>
public interface IDocumentTargetingService
{
    /// <summary>
    /// Sjekker om en bruker har tilgang til et dokument. Returnerer Success hvis
    /// <paramref name="bypassTargeting"/> er true, <paramref name="userId"/> er null,
    /// eller brukeren er i dokumentets målgruppe.
    /// </summary>
    Task<Result> CheckAccessAsync(
        Document document, Guid? userId, bool bypassTargeting, CancellationToken ct);

    /// <summary>
    /// Validerer at target-listene er konsistente med dokumenttypens TargetMode.
    /// Ved opprettelse (isCreate=true) kreves at påkrevde lister har minst ett element.
    /// Ved oppdatering (isCreate=false) sjekkes kun at regler ikke brytes.
    /// </summary>
    Result ValidateTarget(
        DocumentType documentType,
        List<Guid> targetDepartmentIds,
        List<Guid> targetJobTitleIds,
        bool isCreate);

    /// <summary>
    /// Sjekker om en bruker har tilgang til et dokument basert på målgruppe.
    /// TargetMode None = alle kan se. Department/JobTitle = bruker må matche minst én i listen.
    /// Hvis begge lister er satt, må brukeren matche minst én i HVER liste (AND-logikk).
    /// </summary>
    bool CanUserAccessDocument(Document document, Guid? userDepartmentId, Guid? userJobTitleId);

    /// <summary>
    /// Validerer at alle oppgitte avdelinger finnes i databasen.
    /// Returnerer alle avdelinger med hierarki for videre tilgangssjekk.
    /// </summary>
    Task<Result<IReadOnlyList<Department>>> GetAndValidateDepartmentsExistAsync(
        Guid userId, List<Guid> departmentIds, CancellationToken ct);

    /// <summary>
    /// Sjekker at bruker har tilgang til å legge til/fjerne de oppgitte avdelingene.
    /// Brukerens avdeling + alle underavdelinger er tillatt.
    /// </summary>
    Task<Result> CheckDepartmentPermissionAsync(
        Guid userId,
        IReadOnlyList<Department> allDepartments,
        List<Guid> addedDepartmentIds,
        List<Guid> removedDepartmentIds,
        bool bypassTargeting,
        CancellationToken ct);

    /// <summary>
    /// Validerer at alle oppgitte jobbtitler finnes og er aktive.
    /// </summary>
    Task<Result> ValidateJobTitlesExistAsync(
        List<Guid> jobTitleIds, CancellationToken ct);

    /// <summary>
    /// Finner en brukers avdeling og alle underavdelinger i hierarkiet.
    /// </summary>
    IReadOnlySet<Guid> GetDepartmentAndDescendantIds(
        IReadOnlyList<Department> allDepartments, Guid departmentId);
}