using CompVault.Backend.Domain.Entities.Equipment;
using CompVault.Backend.Domain.Entities.Identity;
using CompVault.Backend.Infrastructure.Data;

using Microsoft.EntityFrameworkCore;

namespace CompVault.Backend.SeedData.Seeders;

public static class EquipmentSeeder
{
    public static async Task SeedAsync(AppDbContext dbContext, ILogger logger)
    {
        // Kategorier
        var categoryIds = new Dictionary<string, Guid>(StringComparer.OrdinalIgnoreCase);
        foreach ((string name, string description) in BarnehageData.EquipmentCategories)
        {
            EquipmentCategory? existing = await dbContext.EquipmentCategories
                .FirstOrDefaultAsync(c => c.Name == name);
            if (existing is not null)
            {
                categoryIds[name] = existing.Id;
                continue;
            }

            var category = new EquipmentCategory
            {
                Name = name,
                Description = description,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.EquipmentCategories.Add(category);
            await dbContext.SaveChangesAsync();
            categoryIds[name] = category.Id;
            logger.LogDebug("[Seeder] Utstyrskategori opprettet: {Name}", name);
        }

        // Items
        var itemIds = new Dictionary<(string Category, string Name), Guid>();
        foreach ((string categoryName, string name, bool hasSize) in BarnehageData.EquipmentItems)
        {
            if (!categoryIds.TryGetValue(categoryName, out Guid categoryId))
                continue;

            EquipmentItem? existing = await dbContext.EquipmentItems
                .FirstOrDefaultAsync(i => i.Name == name && i.CategoryId == categoryId);
            if (existing is not null)
            {
                itemIds[(categoryName, name)] = existing.Id;
                continue;
            }

            var item = new EquipmentItem
            {
                CategoryId = categoryId,
                Name = name,
                HasSize = hasSize,
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.EquipmentItems.Add(item);
            await dbContext.SaveChangesAsync();
            itemIds[(categoryName, name)] = item.Id;
            logger.LogDebug("[Seeder] Utstyr opprettet: {Name} (kategori: {Category})", name, categoryName);
        }

        // Utleveringer
        foreach ((string userEmail, string itemName, int quantity, string? size, string issuedByEmail) in BarnehageData.EquipmentIssuances)
        {
            ApplicationUser? user = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == userEmail);
            ApplicationUser? issuedBy = await dbContext.Users
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(u => u.Email == issuedByEmail);

            if (user is null || issuedBy is null)
            {
                logger.LogWarning("[Seeder] Bruker eller utsteder ikke funnet for utlevering: {Email} / {Issuer}", userEmail, issuedByEmail);
                continue;
            }

            // Finn itemId ved å matche navn
            KeyValuePair<(string Category, string Name), Guid> match = itemIds.FirstOrDefault(kvp =>
                string.Equals(kvp.Key.Name, itemName, StringComparison.OrdinalIgnoreCase));
            if (match.Value == Guid.Empty)
            {
                logger.LogWarning("[Seeder] Utstyr ikke funnet: {Item}", itemName);
                continue;
            }

            Guid itemId = match.Value;

            bool exists = await dbContext.EquipmentIssuances
                .AnyAsync(i => i.UserId == user.Id && i.ItemId == itemId);
            if (exists)
                continue;

            var issuance = new EquipmentIssuance
            {
                UserId = user.Id,
                ItemId = itemId,
                Quantity = quantity,
                Size = size,
                IssuedById = issuedBy.Id,
                IssuedDate = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
            };

            dbContext.EquipmentIssuances.Add(issuance);
            await dbContext.SaveChangesAsync();
            logger.LogDebug("[Seeder] Utlevering opprettet: {Item} x{Qty} til {User}", itemName, quantity, userEmail);
        }
    }
}
