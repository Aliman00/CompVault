using CompVault.Backend.Infrastructure.Repositories.Equipment;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Implementerer administrasjon av utstyrskategorier.
/// </summary>
public sealed class EquipmentCategoryService(
    IEquipmentCategoryRepository categoryRepository) : IEquipmentCategoryService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EquipmentCategoryDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Entities.Equipment.EquipmentCategory> categories =
            await categoryRepository.GetAllWithItemsAsync(cancellationToken);

        var dtos = categories.Select(EquipmentMapper.ToDto).ToList();

        return Result<IReadOnlyList<EquipmentCategoryDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentCategoryDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Equipment.EquipmentCategory? category =
            await categoryRepository.GetByIdWithItemsAsync(id, cancellationToken);

        if (category is null)
            return Result<EquipmentCategoryDto>.Failure(
                AppError.NotFound($"Kategori med ID '{id}' ble ikke funnet."));

        return Result<EquipmentCategoryDto>.Success(EquipmentMapper.ToDto(category));
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentCategoryDto>> CreateAsync(
        CreateEquipmentCategoryRequest request, CancellationToken cancellationToken = default)
    {
        bool nameExists = await categoryRepository.ExistsAsync(
            c => c.Name == request.Name, cancellationToken);

        if (nameExists)
            return Result<EquipmentCategoryDto>.Failure(
                AppError.Conflict("Navnet finnes allerede."));

        var category = new Domain.Entities.Equipment.EquipmentCategory
        {
            Name = request.Name,
            Description = request.Description,
            IsActive = true
        };

        await categoryRepository.AddAsync(category, cancellationToken);
        await categoryRepository.SaveChangesAsync(cancellationToken);

        Domain.Entities.Equipment.EquipmentCategory? created =
            await categoryRepository.GetByIdWithItemsAsync(category.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Kategori med ID '{category.Id}' ble ikke funnet etter opprettelse.");

        return Result<EquipmentCategoryDto>.Success(EquipmentMapper.ToDto(created));
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentCategoryDto>> UpdateAsync(
        Guid id, UpdateEquipmentCategoryRequest request, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Equipment.EquipmentCategory? category =
            await categoryRepository.GetByIdAsync(id, cancellationToken);

        if (category is null)
            return Result<EquipmentCategoryDto>.Failure(
                AppError.NotFound($"Kategori med ID '{id}' ble ikke funnet."));

        if (request.Name is not null)
        {
            if (string.IsNullOrWhiteSpace(request.Name))
                return Result<EquipmentCategoryDto>.Failure(
                    AppError.Create(ErrorCode.Validation, "Navn kan ikke være tomt."));

            bool nameExists = await categoryRepository.ExistsAsync(
                c => c.Id != id && c.Name == request.Name, cancellationToken);

            if (nameExists)
                return Result<EquipmentCategoryDto>.Failure(
                    AppError.Conflict("Navnet finnes allerede."));

            category.Name = request.Name;
        }

        if (request.Description is not null)
            category.Description = request.Description;

        if (request.IsActive.HasValue)
            category.IsActive = request.IsActive.Value;

        await categoryRepository.SaveChangesAsync(cancellationToken);

        Domain.Entities.Equipment.EquipmentCategory? updated =
            await categoryRepository.GetByIdWithItemsAsync(category.Id, cancellationToken)
            ?? throw new InvalidOperationException($"Kategori med ID '{category.Id}' ble ikke funnet etter oppdatering.");

        return Result<EquipmentCategoryDto>.Success(EquipmentMapper.ToDto(updated));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Equipment.EquipmentCategory? category =
            await categoryRepository.GetByIdWithItemsForUpdateAsync(id, cancellationToken);

        if (category is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Kategori med ID '{id}' ble ikke funnet."));

        if (category.Items.Any(i => i.IsActive))
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Kan ikke slette en kategori som fortsatt har aktivt utstyr. Deaktiver eller slett utstyret først."));

        await categoryRepository.SoftDeleteAsync(category, cancellationToken);
        await categoryRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}