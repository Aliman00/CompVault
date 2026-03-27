using CompVault.Backend.Domain.Entities.Competencies;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Competencies;

/// <summary>
/// EF Core-konfigurasjon for kompetansetypetabellen.
/// Definerer kolonner, indekser, standardverdier og query filter for soft delete.
/// </summary>
internal sealed class CompetencyTypeConfiguration : IEntityTypeConfiguration<CompetencyType>
{
    public void Configure(EntityTypeBuilder<CompetencyType> builder)
    {
        builder.HasKey(ct => ct.Id);

        builder.Property(ct => ct.Name).HasMaxLength(200).IsRequired();
        builder.Property(ct => ct.Description).HasMaxLength(500);
        builder.Property(ct => ct.Category).HasMaxLength(100);
        builder.Property(ct => ct.RequiresExpiration).IsRequired().HasDefaultValue(true);
        builder.Property(ct => ct.CreatedAt).IsRequired();
        builder.Property(ct => ct.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(ct => ct.Category);
        builder.HasIndex(ct => ct.DeletedAt);

        builder.HasQueryFilter(ct => ct.DeletedAt == null);
    }
}