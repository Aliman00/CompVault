using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.DTOs.Departments;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Departments.Services;

/// <summary>
/// Implementerer avdelingsadministrasjon.
/// </summary>
public sealed class DepartmentService(
    IDepartmentRepository departmentRepository) : IDepartmentService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<DepartmentDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Department> departments = await departmentRepository.GetAllWithHierarchyAsync(cancellationToken);

        var dtos = departments
            .Select(d => DepartmentMapper.ToDto(d, d.SubDepartments.Count, d.Members.Count))
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
            DepartmentMapper.ToDto(department, department.SubDepartments.Count, department.Members.Count));
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> CreateAsync(CreateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ParentDepartmentId.HasValue)
        {
            bool parentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.ParentDepartmentId.Value, cancellationToken);

            if (!parentExists)
                return Result<DepartmentDto>.Failure(
                    AppError.NotFound($"Overordnet avdeling med ID '{request.ParentDepartmentId}' ble ikke funnet."));
        }

        var department = new Department
        {
            Name = request.Name,
            Description = request.Description ?? string.Empty,
            ParentDepartmentId = request.ParentDepartmentId,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        await departmentRepository.AddAsync(department, cancellationToken);
        await departmentRepository.SaveChangesAsync(cancellationToken);

        return Result<DepartmentDto>.Success(DepartmentMapper.ToDto(department, 0, 0));
    }

    /// <inheritdoc />
    public async Task<Result<DepartmentDto>> UpdateAsync(Guid id, UpdateDepartmentRequest request, CancellationToken cancellationToken = default)
    {
        Department? department = await departmentRepository.GetByIdWithHierarchyAsync(id, cancellationToken);

        if (department is null)
            return Result<DepartmentDto>.Failure(
                AppError.NotFound($"Avdeling med ID '{id}' ble ikke funnet."));

        if (request.Name is not null)
            department.Name = request.Name;

        if (request.Description is not null)
            department.Description = request.Description;

        if (request.IsActive.HasValue)
            department.IsActive = request.IsActive.Value;

        if (request.ParentDepartmentId.HasValue)
        {
            if (request.ParentDepartmentId.Value == id)
                return Result<DepartmentDto>.Failure(
                    AppError.Create(ErrorCode.Validation, "En avdeling kan ikke være sin egen forelder."));

            bool parentExists = await departmentRepository.ExistsAsync(
                d => d.Id == request.ParentDepartmentId.Value, cancellationToken);

            if (!parentExists)
                return Result<DepartmentDto>.Failure(
                    AppError.NotFound($"Overordnet avdeling med ID '{request.ParentDepartmentId}' ble ikke funnet."));

            IReadOnlyList<Guid> ancestorIds = await departmentRepository.GetAncestorIdsAsync(id, cancellationToken);
            if (ancestorIds.Contains(request.ParentDepartmentId.Value))
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

        return Result<DepartmentDto>.Success(
            DepartmentMapper.ToDto(department, department.SubDepartments.Count, department.Members.Count));
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

        await departmentRepository.SoftDeleteAsync(department, cancellationToken);

        return Result<bool>.Success(true);
    }
}
