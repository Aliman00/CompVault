using CompVault.Backend.Domain.Entities.Departments;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Departments;

/// <summary>
/// EF Core-konfigurasjon for avdelingstabellen.
/// Støtter hierarki via ParentDepartmentId (en avdeling kan ha underavdelinger).
/// </summary>
internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Name).HasMaxLength(200).IsRequired();
        builder.Property(d => d.Description).HasMaxLength(500);
        builder.Property(d => d.CreatedAt).IsRequired();
        builder.Property(d => d.IsActive).IsRequired().HasDefaultValue(true);

        builder.HasIndex(d => d.DeletedAt);

        builder.HasQueryFilter(d => d.DeletedAt == null);

        // Selvrefererende relasjon for avdelingshierarki
        builder.HasOne(d => d.ParentDepartment)
            .WithMany(d => d.SubDepartments)
            .HasForeignKey(d => d.ParentDepartmentId)
            .OnDelete(DeleteBehavior.SetNull);

        // Referer til ApplicationUser som har opprettet avdelingen
        builder.HasOne(d => d.CreatedBy)
            .WithMany()
            .HasForeignKey(d => d.CreatedById)
            .OnDelete(DeleteBehavior.SetNull);
    }
}