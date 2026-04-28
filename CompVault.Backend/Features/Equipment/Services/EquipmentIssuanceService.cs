using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Features.Departments.Services;
using CompVault.Backend.Infrastructure.Repositories.Equipment;
using CompVault.Backend.Infrastructure.Repositories.Identity;
using CompVault.Shared.Constants;
using CompVault.Shared.Constants.Validations;
using CompVault.Shared.DTOs.Common.Pagination;
using CompVault.Shared.DTOs.Equipment;
using CompVault.Shared.Result;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.Features.Equipment.Services;

/// <summary>
/// Implementerer administrasjon av utleveringer.
/// </summary>
public sealed class EquipmentIssuanceService(
    IEquipmentIssuanceRepository issuanceRepository,
    IEquipmentItemRepository itemRepository,
    IUserRepository userRepository,
    IDepartmentScopeService departmentScope,
    ILogger<EquipmentIssuanceService> logger) : IEquipmentIssuanceService
{
    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentIssuanceDto>>> GetAllAsync(
        PagedQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<EquipmentIssuance> baseQuery = issuanceRepository.QueryWithDetails();

        int totalCount = await baseQuery.CountAsync(cancellationToken);

        List<EquipmentIssuance> issuances = await baseQuery
            .OrderByDescending(i => i.IssuedDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = issuances.Select(EquipmentMapper.ToDto).ToList();

        return Result<PagedResult<EquipmentIssuanceDto>>.Success(
            PagedResult<EquipmentIssuanceDto>.Create(dtos, totalCount, query));
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentIssuanceDto>> GetByIdAsync(
        Guid id, CancellationToken cancellationToken = default)
    {
       EquipmentIssuance? issuance =
            await issuanceRepository.GetByIdWithDetailsAsync(id, cancellationToken);

        if (issuance is null)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.NotFound($"Utlevering med ID '{id}' ble ikke funnet."));

        return Result<EquipmentIssuanceDto>.Success(EquipmentMapper.ToDto(issuance));
    }

    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentIssuanceDto>>> GetByUserAsync(
        Guid userId, PagedQuery query, CancellationToken cancellationToken = default)
    {
        IQueryable<EquipmentIssuance> baseQuery = issuanceRepository.QueryWithDetails()
            .Where(i => i.UserId == userId);
        int totalCount = await baseQuery.CountAsync(cancellationToken);

        List<EquipmentIssuance> issuances = await baseQuery
            .OrderByDescending(i => i.IssuedDate)
            .Skip(query.Skip)
            .Take(query.PageSize)
            .ToListAsync(cancellationToken);

        var dtos = issuances.Select(EquipmentMapper.ToDto).ToList();

        return Result<PagedResult<EquipmentIssuanceDto>>.Success(
            PagedResult<EquipmentIssuanceDto>.Create(dtos, totalCount, query));
    }
    
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<EquipmentIssuanceDto>>> GetByItemAsync(Guid equipmentItemId, 
        CancellationToken ct = default)
    {
        bool itemExists = await itemRepository.ExistsAsync(u => u.Id == equipmentItemId, ct);

        if (!itemExists)
            return Result<IReadOnlyList<EquipmentIssuanceDto>>.Failure(
                AppError.NotFound($"Utstyret ble ikke funnet"));

        IReadOnlyList<EquipmentIssuance> issuances =
            await issuanceRepository.GetByItemIdAsync(equipmentItemId, ct);

        var dtos = issuances.Select(EquipmentMapper.ToDto).ToList();

        return Result<IReadOnlyList<EquipmentIssuanceDto>>.Success(dtos);
    }
    
    /// <inheritdoc />
    public async Task<Result<IReadOnlyList<UserEquipmentCategoryDto>>> GetCategoriesForUserAsync(Guid userId, 
        CancellationToken ct = default)
    {
        bool userExists = await userRepository.ExistsAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            logger.LogWarning("Bruker med ID {UserId} ble ikke funnet ved henting av utstyrskategorier", userId);
            return Result<IReadOnlyList<UserEquipmentCategoryDto>>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));
        }

        IReadOnlyList<UserEquipmentCategoryDto> result =
            await issuanceRepository.GetCategoriesForUserAsync(userId, ct);

        return Result<IReadOnlyList<UserEquipmentCategoryDto>>.Success(result);
    }
    
    /// <inheritdoc />
    public async Task<Result<PagedResult<EquipmentIssuanceDto>>> GetMyEquipmentAsync(Guid userId, Guid? categoryId, 
        PagedQuery query, CancellationToken ct = default)
    {
        bool userExists = await userRepository.ExistsAsync(u => u.Id == userId, ct);
        if (!userExists)
        {
            logger.LogWarning("Bruker med ID {UserId} ble ikke funnet ved henting av utstyr", userId);
            return Result<PagedResult<EquipmentIssuanceDto>>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));
        }

        (IReadOnlyList<EquipmentIssuance> items, int totalCount) = 
            await issuanceRepository.GetByUserIdPagedAsync(userId, categoryId, query, ct);

        var equipmentIssuanceDtos = items.Select(EquipmentMapper.ToDto).ToList();

        return Result<PagedResult<EquipmentIssuanceDto>>.Success(
            PagedResult<EquipmentIssuanceDto>.Create(equipmentIssuanceDtos, totalCount, query));
    }

    /// <inheritdoc />
    public async Task<Result<EquipmentIssuanceDto>> CreateAsync(
        Guid issuedById, CreateEquipmentIssuanceRequest request, CancellationToken cancellationToken = default)
    {
        // Valider input-GUIDs
        if (request.UserId == Guid.Empty)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Ugyldig bruker-ID."));

        if (request.ItemId == Guid.Empty)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Ugyldig utstyr-ID."));

        if (issuedById == Guid.Empty)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Ugyldig ID for utsteder."));

        Guid userId = request.UserId;
        Guid itemId = request.ItemId;
        DateTime issuedDate = request.IssuedDate;

        if (request.IssuedDate > DateTime.UtcNow.AddDays(1))
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Utleveringsdato kan ikke være i fremtiden."));

        if (request.IssuedDate < DateTime.UtcNow.AddYears(-1))
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation, "Utleveringsdato kan ikke være mer enn 1 år tilbake i tid."));

        // Valider bruker
        ApplicationUser? recipient = await userRepository.GetByIdIgnoringFiltersAsync(userId, cancellationToken);
        if (recipient is null)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.NotFound($"Bruker med ID '{userId}' ble ikke funnet."));
        
        // Sjekker at brukeren har tilattelse for å opprette en utlevering for denne brukeren
        if (!departmentScope.IsAllowed(recipient.DepartmentId, Permissions.EquipmentAll, 
                Permissions.EquipmentReadSub))
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Forbidden, 
                    "Du har ikke tilgang til å opprette utleveringer for denne brukeren."));

        // Valider utstyr
        EquipmentItem? item =
            await itemRepository.GetByIdWithCategoryAsync(itemId, cancellationToken);

        if (item is null)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.NotFound($"Utstyr med ID '{itemId}' ble ikke funnet."));

        if (!item.IsActive)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation,
                    $"Utstyret '{item.Name}' er inaktivt og kan ikke utleveres."));

        if (item.HasSize && string.IsNullOrWhiteSpace(request.Size))
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.Validation, EquipmentValidations.Errors.SizeRequired));

        if (!item.HasSize && !string.IsNullOrWhiteSpace(request.Size))
            request.Size = null;

        // Valider utsteder
        bool issuerExists = await userRepository.ExistsAsync(u => u.Id == issuedById, cancellationToken);

        if (!issuerExists)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.NotFound($"Utsteder med ID '{issuedById}' ble ikke funnet."));

        var issuance = new EquipmentIssuance
        {
            UserId = userId,
            ItemId = itemId,
            Quantity = request.Quantity,
            Size = request.Size,
            IssuedById = issuedById,
            IssuedDate = issuedDate,
            Notes = request.Notes,
            IsActive = true
        };

        await issuanceRepository.AddAsync(issuance, cancellationToken);
        await issuanceRepository.SaveChangesAsync(cancellationToken);

        EquipmentIssuance? created =
            await issuanceRepository.GetByIdWithDetailsAsync(issuance.Id, cancellationToken);
        if (created is null)
        {
            logger.LogError("Utlevering {IssuanceId} forsvant etter opprettelse", issuance.Id);
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.Create(ErrorCode.InternalError, "Utleveringen ble ikke funnet etter opprettelse."));
        }

        return Result<EquipmentIssuanceDto>.Success(EquipmentMapper.ToDto(created));
    }

    /// <inheritdoc />
    /// <summary>
    /// Oppdaterer en utlevering. Merk: bruker, utstyr, utsteder og utleveringsdato
    /// kan <em>ikke</em> endres — kun antall, størrelse og notater.
    /// For å endre bruker eller utstyr må utleveringen slettes og gjenskapes.
    /// </summary>
    public async Task<Result<EquipmentIssuanceDto>> UpdateAsync(
        Guid id, UpdateEquipmentIssuanceRequest request, CancellationToken cancellationToken = default)
    {
        EquipmentIssuance? issuance =
            await issuanceRepository.GetForUpdateAsync(id, cancellationToken);

        if (issuance is null)
            return Result<EquipmentIssuanceDto>.Failure(
                AppError.NotFound($"Utlevering med ID '{id}' ble ikke funnet."));

        if (request.Quantity.HasValue)
            issuance.Quantity = request.Quantity.Value;

        if (request.Size is not null)
        {
            if (issuance.Item != null && issuance.Item.HasSize && string.IsNullOrWhiteSpace(request.Size))
                return Result<EquipmentIssuanceDto>.Failure(
                    AppError.Create(ErrorCode.Validation, EquipmentValidations.Errors.SizeRequired));

            if (issuance.Item != null && !issuance.Item.HasSize)
                issuance.Size = null;
            else
                issuance.Size = string.IsNullOrWhiteSpace(request.Size) ? null : request.Size.Trim();
        }

        if (request.Notes is not null)
            issuance.Notes = request.Notes;

        await issuanceRepository.SaveChangesAsync(cancellationToken);

        return Result<EquipmentIssuanceDto>.Success(EquipmentMapper.ToDto(issuance));
    }

    /// <inheritdoc />
    public async Task<Result<bool>> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EquipmentIssuance? issuance = await issuanceRepository.GetForUpdateAsync(id, cancellationToken);
        if (issuance is null)
            return Result<bool>.Failure(AppError.NotFound($"Utlevering med ID '{id}' ble ikke funnet."));

        await issuanceRepository.SoftDeleteAsync(issuance, cancellationToken);
        await issuanceRepository.SaveChangesAsync(cancellationToken);

        return Result<bool>.Success(true);
    }
}
