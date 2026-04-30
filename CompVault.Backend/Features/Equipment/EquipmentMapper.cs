using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Shared.DTOs.Equipment;

namespace CompVault.Backend.Features.Equipment;

/// <summary>
/// Mapper for konvertering mellom Equipment-entiteter og DTOs.
/// </summary>
public static class EquipmentMapper
{
    /// <summary>
    /// Konverterer en <see cref="EquipmentCategory"/> til en <see cref="EquipmentCategoryDto"/>.
    /// </summary>
    public static EquipmentCategoryDto ToDto(EquipmentCategory category) => new()
    {
        Id = category.Id,
        Name = category.Name,
        Description = category.Description,
        IsActive = category.IsActive,
        CreatedAt = category.CreatedAt,
        ItemCount = category.Items.Count
    };

    /// <summary>
    /// Konverterer en <see cref="EquipmentItem"/> til en <see cref="EquipmentItemDto"/>.
    /// </summary>
    public static EquipmentItemDto ToDto(EquipmentItem item) => new()
    {
        Id = item.Id,
        CategoryId = item.CategoryId,
        CategoryName = item.Category?.Name,
        Name = item.Name,
        HasSize = item.HasSize,
        IsActive = item.IsActive,
        CreatedAt = item.CreatedAt
    };

    /// <summary>
    /// Konverterer en <see cref="EquipmentIssuance"/> til en <see cref="EquipmentIssuanceDto"/>.
    /// </summary>
    public static EquipmentIssuanceDto ToDto(EquipmentIssuance issuance) => new()
    {
        Id = issuance.Id,
        UserId = issuance.UserId,
        UserName = issuance.User?.UserName,
        UserFirstName = issuance.User?.FirstName,
        UserLastName = issuance.User?.LastName,
        ItemId = issuance.ItemId,
        ItemName = issuance.Item?.Name,
        CategoryId = issuance.Item?.CategoryId,
        CategoryName = issuance.Item?.Category?.Name,
        Quantity = issuance.Quantity,
        Size = issuance.Size,
        HasSize = issuance.Item?.HasSize ?? false,
        IssuedById = issuance.IssuedById,
        IssuedByName = issuance.IssuedBy != null
            ? $"{issuance.IssuedBy.FirstName} {issuance.IssuedBy.LastName}".Trim()
            : null,
        IssuedDate = issuance.IssuedDate,
        Notes = issuance.Notes,
        CreatedAt = issuance.CreatedAt
    };
}