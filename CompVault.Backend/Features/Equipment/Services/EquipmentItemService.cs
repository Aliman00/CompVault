using CompVault.Backend.Infrastructure.Repositories.Equipment;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Implementerer administrasjon av utstyr under kategorier.
/// </summary>
public sealed class EquipmentItemService(
    IEquipmentItemRepository itemRepository,
    IEquipmentCategoryRepository categoryRepository,
    ILogger<EquipmentItemService> logger) : IEquipmentItemService
{
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EquipmentItemDto>>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        IReadOnlyList<Domain.Entities.Equipment.EquipmentItem> items =
            await itemRepository.GetAllWithCategoryAsync(cancellationToken);

        var dtos = items.Select(EquipmentMapper.ToDto).ToList();

        return Result<IReadOnlyList<EquipmentItemDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentItemDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Equipment.EquipmentItem? item =
            await itemRepository.GetByIdWithCategoryAsync(id, cancellationToken);

        if (item is null)
            return Result<EquipmentItemDto>.Failure(
                AppError.NotFound($"Utstyr med ID '{id}' ble ikke funnet."));

        return Result<EquipmentItemDto>.Success(EquipmentMapper.ToDto(item));
    }

    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EquipmentItemDto>>> GetByCategoryAsync(
        Guid categoryId, CancellationToken cancellationToken = default)
    {
        bool categoryExists = await categoryRepository.ExistsAsync(c => c.Id == categoryId, cancellationToken);

        if (!categoryExists)
            return Result<IReadOnlyList<EquipmentItemDto>>.Failure(
                AppError.NotFound($"Kategori med ID '{categoryId}' ble ikke funnet."));

        IReadOnlyList<Domain.Entities.Equipment.EquipmentItem> items =
            await itemRepository.GetByCategoryIdAsync(categoryId, cancellationToken);

        var dtos = items.Select(EquipmentMapper.ToDto).ToList();

        return Result<IReadOnlyList<EquipmentItemDto>>.Success(dtos);
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentItemDto>> CreateAsync(
        CreateEquipmentItemRequest request, CancellationToken cancellationToken = default)
    {
        request.Name = request.Name.Trim();

        if (request.CategoryId == Guid.Empty)
            return Result<EquipmentItemDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Ugyldig kategori-ID."));

        bool nameExists = await itemRepository.ExistsAsync(
            i => i.CategoryId == request.CategoryId && i.Name == request.Name, cancellationToken);

        if (nameExists)
            return Result<EquipmentItemDto>.Failure(
                AppError.Conflict("Navnet finnes allerede i denne kategorien."));

        Domain.Entities.Equipment.EquipmentCategory? category =
            await categoryRepository.GetByIdAsync(request.CategoryId, cancellationToken);

        if (category is null)
            return Result<EquipmentItemDto>.Failure(
                AppError.NotFound($"Kategori med ID '{request.CategoryId}' ble ikke funnet."));

        if (!category.IsActive)
            return Result<EquipmentItemDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Kategorien '{category.Name}' er inaktiv og kan ikke brukes."));

        var item = new Domain.Entities.Equipment.EquipmentItem
        {
            CategoryId = request.CategoryId,
            Name = request.Name,
            HasSize = request.HasSize,
            IsActive = true
        };

        await itemRepository.AddAsync(item, cancellationToken);
        await itemRepository.SaveChangesAsync(cancellationToken);

        Domain.Entities.Equipment.EquipmentItem created =
            await itemRepository.GetByIdWithCategoryAsync(item.Id, cancellationToken);
        if (created is null)
        {
            logger.LogError("Utstyr {ItemId} forsvant etter opprettelse", item.Id);
            return Result<EquipmentItemDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Utstyret ble ikke funnet etter opprettelse."));
        }

        return Result<EquipmentItemDto>.Success(EquipmentMapper.ToDto(created));
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentItemDto>> UpdateAsync(
        Guid id, UpdateEquipmentItemRequest request, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Equipment.EquipmentItem? item =
            await itemRepository.GetByIdAsync(id, cancellationToken);

        if (item is null)
            return Result<EquipmentItemDto>.Failure(
                AppError.NotFound($"Utstyr med ID '{id}' ble ikke funnet."));

        if (request.Name is not null)
        {
            request.Name = request.Name.Trim();

            if (string.IsNullOrWhiteSpace(request.Name))
                return Result<EquipmentItemDto>.Failure(
                    AppError.Create(ErrorCode.Validation, "Navn kan ikke være tomt."));

            bool nameExists = await itemRepository.ExistsAsync(
                i => i.Id != id && i.Name == request.Name && i.CategoryId == item.CategoryId,
                cancellationToken);

            if (nameExists)
                return Result<EquipmentItemDto>.Failure(
                    AppError.Conflict("Navnet finnes allerede i denne kategorien."));

            item.Name = request.Name;
        }

        if (request.HasSize.HasValue)
            item.HasSize = request.HasSize.Value;

        if (request.IsActive.HasValue)
            item.IsActive = request.IsActive.Value;

        await itemRepository.SaveChangesAsync(cancellationToken);

        Domain.Entities.Equipment.EquipmentItem updated =
            await itemRepository.GetByIdWithCategoryAsync(id, cancellationToken);
        if (updated is null)
        {
            logger.LogError("Utstyr {ItemId} forsvant etter oppdatering", id);
            return Result<EquipmentItemDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Utstyret ble ikke funnet etter oppdatering."));
        }

        return Result<EquipmentItemDto>.Success(EquipmentMapper.ToDto(updated));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        Domain.Entities.Equipment.EquipmentItem? item =
            await itemRepository.GetByIdTrackedAsync(id, cancellationToken);

        if (item is null)
            return Result<bool>.Failure(
                AppError.NotFound($"Utstyr med ID '{id}' ble ikke funnet."));

        bool hasActiveIssuances = await itemRepository.HasActiveIssuancesAsync(id, cancellationToken);
        if (hasActiveIssuances)
            return Result<bool>.Failure(
                AppError.Create(ErrorCode.Validation,
                    "Kan ikke slette utstyr som har aktive utleveringer. Slett utleveringene først."));

        await itemRepository.SoftDeleteAsync(item, cancellationToken);
        await itemRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}