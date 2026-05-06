using CompVault.Backend.Domain.Entities.Departments;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Domain.Entities.JobTitles;
using CompVault.Backend.Infrastructure.Repositories.Departments;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Backend.Infrastructure.Repositories.JobTitles;
using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Departments.Services;

/// <summary>
/// Implementerer avdelingsadministrasjon.
/// </summary>
public sealed class DepartmentService(
    IDepartmentRepository departmentRepository,
    IUserRepository userRepository,
    IJobTitleRepository jobTitleRepository,
    ILogger<DepartmentService> logger) : IDepartmentService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DepartmentDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Department> departments = await departmentRepository.GetAllWithHierarchyAsync(cancellationToken);

        var dtos = departments
            .Select(d => DepartmentMapper.ToDto(d, d.SubDepartments.Count))
            .ToList();

        return Result<IReadOnlyList<DepartmentDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Department? department = await departmentRepository.GetByIdWithHierarchyAsync(id, cancellationToken);

        if (department is null)
            return Result<DepartmentDto>.Failure(
                AppError.NotFound($"Avdeling med ID '{id}' ble ikke funnet."));

        return Result<DepartmentDto>.Success(
            DepartmentMapper.ToDto(department, department.SubDepartments.Count));
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> CreateAsync(Guid userId, CreateDepartmentRequest request,
        CancellationToken ct = default)
    {
        if (request.ParentDepartmentId.HasValue)
        {
            bool parentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.ParentDepartmentId.Value && d.IsActive, ct);

            if (!parentExists)
                return Result<DepartmentDto>.Failure(
                    AppError.NotFound($"Overordnet avdeling med ID '{request.ParentDepartmentId}' ble ikke funnet."));
        }

        if (request.ManagerId.HasValue && !await IsValidManagerAsync(request.ManagerId.Value, ct))
        {
            return Result<DepartmentDto>.Failure(
                AppError.NotFound($"Leder med ID '{request.ManagerId.Value}' ble ikke funnet, er inaktiv, eller har ikke lederstilling."));
        }

        var department = new Department
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            ParentDepartmentId = request.ParentDepartmentId,
            ManagerId = request.ManagerId,
            CreatedAt = DateTime.UtcNow,
            CreatedById = userId,
            IsActive = true
        };

        await departmentRepository.AddAsync(department, ct);
        await departmentRepository.SaveChangesAsync(ct);

        return Result<DepartmentDto>.Success(DepartmentMapper.ToDto(department, 0));
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        Department? department = await departmentRepository.GetByIdAsync(id, cancellationToken);

        if (department is null)
            return Result<DepartmentDto>.Failure(
                AppError.NotFound($"Avdeling med ID '{id}' ble ikke funket."));

        if (request.Name is not null)
            department.Name = request.Name;

        if (request.Description is not null)
            department.Description = request.Description;

        if (request.IsActive.HasValue)
            department.IsActive = request.IsActive.Value;

        if (request.ManagerId.HasValue && !await IsValidManagerAsync(request.ManagerId.Value, cancellationToken))
        {
            return Result<DepartmentDto>.Failure(
                AppError.NotFound($"Leder med ID '{request.ManagerId.Value}' ble ikke funnet, er inaktiv, eller har ikke lederstilling."));
        }

        if (request.ManagerId.HasValue)
            department.ManagerId = request.ManagerId;
        else if (request.ClearManagerId)
            department.ManagerId = null;

        if (request.ParentDepartmentId.HasValue)
        {
            if (request.ParentDepartmentId.Value == id)
                return Result<DepartmentDto>.Failure(
                    AppError.Create(ErrorCode.Validation, "En avdeling kan ikke være sin egen forelder."));

            bool parentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.ParentDepartmentId.Value && d.IsActive, cancellationToken);

            if (!parentExists)
                return Result<DepartmentDto>.Failure(
                    AppError.NotFound($"Overordnet avdeling med ID '{request.ParentDepartmentId}' ble ikke funnet."));

            IReadOnlyList<Guid> ancestorIds = await departmentRepository.GetAncestorIdsAsync(request.ParentDepartmentId.Value, cancellationToken);
            if (ancestorIds.Contains(id))
                return Result<DepartmentDto>.Failure(
                    AppError.Create(ErrorCode.Validation, "Kan ikke sette en underavdeling til å være forelder."));

            department.ParentDepartmentId = request.ParentDepartmentId.Value;
        }
        else if (request.ClearParentDepartment)
        {
            department.ParentDepartmentId = null;
        }

        await departmentRepository.UpdateAsync(department, cancellationToken);
        await departmentRepository.SaveChangesAsync(cancellationToken);

        Department? updatedDepartment = await departmentRepository.GetByIdWithHierarchyAsync(id, cancellationToken);
        if (updatedDepartment is null)
        {
            logger.LogError("Avdeling {DepartmentId} forsvant etter oppdatering", id);
            return Result<DepartmentDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Avdelingen ble ikke funnet etter oppdatering."));
        }

        return Result<DepartmentDto>.Success(
            DepartmentMapper.ToDto(updatedDepartment, updatedDepartment.SubDepartments.Count));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Department? department = await departmentRepository.GetByIdAsync(id, cancellationToken);

        if (department is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Avdeling med ID '{id}' ble ikke funnet."));

        bool hasSubDepartments = await departmentRepository.HasSubDepartmentsAsync(id, cancellationToken);
        if (hasSubDepartments)
            return Result<bool>.Failure(
                AppError.Conflict("Kan ikke slette en avdeling som har underavdelinger."));

        bool hasMembers = await departmentRepository.HasMembersAsync(id, cancellationToken);
        if (hasMembers)
            return Result<bool>.Failure(
                AppError.Conflict("Kan ikke slette en avdeling som har medlemmer."));

        await departmentRepository.SoftDeleteAsync(department);
        await departmentRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }

    /// <summary>
    /// Sjekker at en bruker kan være leder for en avdeling:
    /// - Brukeren må eksistere og være aktiv
    /// - Brukeren må ha en stillingstittel med IsLeader=true
    /// </summary>
    private async Task<bool> IsValidManagerAsync(Guid userId, CancellationToken ct)
    {
        ApplicationUser? user = await userRepository.GetByIdIgnoringFiltersAsync(userId, ct);
        if (user is null || user.DeletedAt is not null || !user.IsActive)
            return false;

        if (!user.JobTitleId.HasValue)
            return false;

        JobTitle? jobTitle = await jobTitleRepository.GetByIdAsync(user.JobTitleId.Value, ct);
        return jobTitle is not null && jobTitle.IsLeader;
    }
}