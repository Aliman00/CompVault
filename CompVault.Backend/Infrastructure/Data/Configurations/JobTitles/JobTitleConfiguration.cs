using CompVault.Backend.Domain.Entities.JobTitles;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.JobTitles;

/// <summary>
/// EF Core-konfigurasjon for JobTitle-tabellen.
/// </summary>
internal sealed class JobTitleConfiguration : IEntityTypeConfiguration<JobTitle>
{
    public void Configure(EntityTypeBuilder<JobTitle> builder)
    {
        builder.Property(j => j.Name).HasMaxLength(100).IsRequired();
        builder.Property(j => j.IsActive).IsRequired().HasDefaultValue(true);
        builder.Property(j => j.CreatedAt).IsRequired();

        // Unikt navn — forhindrer duplikate jobbtitler
        builder.HasIndex(j => j.Name).IsUnique();

        builder.HasIndex(j => j.DeletedAt);

        // Soft-delete filter
        builder.HasQueryFilter(j => j.DeletedAt == null);
    }
}