using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Documents;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
using CompVault.Shared.Enums;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Documents.Services;

/// <inheritdoc />
public sealed class DocumentTargetingService(
    IDepartmentRepository departmentRepository,
    IJobTitleRepository jobTitleRepository,
    IUserRepository userRepository,
    ILogger<DocumentTargetingService> logger) : IDocumentTargetingService
{
    /// <inheritdoc />
    public async Task<Result> CheckAccessAsync(
        Document document, Guid? userId, bool bypassTargeting, CancellationToken ct)
    {
        if (bypassTargeting || !userId.HasValue)
            return Result.Success();

        ApplicationUser? user = await userRepository.GetByIdAsync(userId.Value, ct);
        if (!CanUserAccessDocument(document, user?.DepartmentId, user?.JobTitleId))
            return Result.Failure(
                AppError.Create(ErrorCode.Forbidden, "Du har ikke tilgang til dette dokumentet."));

        return Result.Success();
    }

    /// <inheritdoc />
    public Result ValidateTarget(
        DocumentType documentType,
        List<Guid> targetDepartmentIds,
        List<Guid> targetJobTitleIds,
        bool isCreate)
    {
        bool hasDepartments = targetDepartmentIds.Count > 0;
        bool hasJobTitles = targetJobTitleIds.Count > 0;

        return documentType.TargetMode switch
        {
            DocumentTargetMode.None when hasDepartments || hasJobTitles =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' har TargetMode=None. Target-lister kan ikke settes.")),
            DocumentTargetMode.Department when isCreate && !hasDepartments =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever minst én målavdeling (TargetDepartmentIds).")),
            DocumentTargetMode.Department when hasJobTitles && !hasDepartments =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetDepartmentIds er satt når TargetJobTitleIds brukes.")),
            DocumentTargetMode.JobTitle when isCreate && !hasJobTitles =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever minst én mål-jobbtittel (TargetJobTitleIds).")),
            DocumentTargetMode.JobTitle when hasDepartments && !hasJobTitles =>
                Result.Failure(AppError.Create(ErrorCode.Validation,
                    $"Dokumenttype '{documentType.Name}' krever at TargetJobTitleIds er satt når TargetDepartmentIds brukes.")),
            _ => Result.Success()
        };
    }

    /// <inheritdoc />
    public bool CanUserAccessDocument(Document document, Guid? userDepartmentId, Guid? userJobTitleId)
    {
        // Ingen målgruppe = alle kan se
        if (document.DocumentDepartments.Count == 0 && document.DocumentJobTitles.Count == 0)
            return true;

        // Hvis avdelingsmålgruppe er satt, må brukeren matche minst én avdeling
        bool departmentMatch = document.DocumentDepartments.Count == 0 ||
            (userDepartmentId.HasValue && document.DocumentDepartments.Any(dd => dd.DepartmentId == userDepartmentId.Value));

        // Hvis jobbtittel-målgruppe er satt, må brukeren matche minst én jobbtittel
        bool jobTitleMatch = document.DocumentJobTitles.Count == 0 ||
            (userJobTitleId.HasValue && document.DocumentJobTitles.Any(dj => dj.JobTitleId == userJobTitleId.Value));

        // AND-logikk mellom kategoriene: hvis begge er satt, må begge matche
        return departmentMatch && jobTitleMatch;
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<Department>>> GetAndValidateDepartmentsExistAsync(
        Guid userId, List<Guid> departmentIds, CancellationToken ct)
    {
        IReadOnlyList<Department> allDepartments =
            await departmentRepository.GetAllWithHierarchyAsync(ct);

        var existingDepartmentIds = allDepartments.Select(d => d.Id).ToHashSet();
        var missingIds = departmentIds
            .Where(id => !existingDepartmentIds.Contains(id))
            .ToList();

        if (missingIds.Count > 0)
        {
            logger.LogWarning("Bruker {UserId} prøvde å legge til avdeling {DepartmentId} som ikke finnes",
                userId, missingIds.First());
            return Result<IReadOnlyList<Department>>.Failure(
                AppError.NotFound($"Avdeling med ID '{missingIds.First()}' ble ikke funnet."));
        }

        return Result<IReadOnlyList<Department>>.Success(allDepartments);
    }

    /// <inheritdoc />
    public async Task<Result> CheckDepartmentPermissionAsync(
        Guid userId,
        IReadOnlyList<Department> allDepartments,
        List<Guid> addedDepartmentIds,
        List<Guid> removedDepartmentIds,
        bool bypassTargeting,
        CancellationToken ct)
    {
        if (bypassTargeting)
            return Result.Success();

        ApplicationUser? user = await userRepository.GetByIdAsync(userId, ct);
        if (user?.DepartmentId is null)
        {
            logger.LogWarning("Bruker {UserId} har ingen tilknyttet avdeling", userId);
            return Result.Failure(
                AppError.Create(ErrorCode.Forbidden, "Bruker har ingen tilknyttet avdeling"));
        }

        IReadOnlySet<Guid> allowedIds = GetDepartmentAndDescendantIds(allDepartments, user.DepartmentId);

        var forbiddenIds = addedDepartmentIds.Concat(removedDepartmentIds)
            .Where(guid => !allowedIds.Contains(guid))
            .ToList();

        if (forbiddenIds.Count > 0)
        {
            logger.LogWarning("Bruker {UserId} prøvde å endre avdelinger uten tilgang: {ForbiddenIds}",
                userId, string.Join(", ", forbiddenIds));
            return Result.Failure(
                AppError.Create(ErrorCode.ForbiddenDepartment,
                    $"Du har ikke tilgang til følgende avdelinger: {string.Join(", ", forbiddenIds)}"));
        }

        return Result.Success();
    }

    /// <inheritdoc />
    public async Task<Result> ValidateJobTitlesExistAsync(List<Guid> jobTitleIds, CancellationToken ct)
    {
        if (jobTitleIds.Count == 0)
            return Result.Success();

        var existingJtIds = (await jobTitleRepository.FindAsync(
            j => jobTitleIds.Contains(j.Id) && j.IsActive, ct))
            .Select(j => j.Id).ToHashSet();
        var missing = jobTitleIds.Except(existingJtIds).ToList();

        if (missing.Count > 0)
            return Result.Failure(
                AppError.NotFound($"Jobbtittel med ID '{missing.First()}' ble ikke funnet."));

        return Result.Success();
    }

    /// <inheritdoc />
    public IReadOnlySet<Guid> GetDepartmentAndDescendantIds(
        IReadOnlyList<Department> allDepartments, Guid departmentId)
    {
        var allowedDepartments = new HashSet<Guid>();
        var queue = new Queue<Guid>();
        queue.Enqueue(departmentId);

        while (queue.Count > 0)
        {
            Guid current = queue.Dequeue();
            allowedDepartments.Add(current);

            foreach (Department childDepartment in allDepartments.Where(d => d.ParentDepartmentId == current))
                queue.Enqueue(childDepartment.Id);
        }

        return allowedDepartments;
    }
}