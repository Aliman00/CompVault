using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for DocumentDepartment-koblingstabellen.
/// </summary>
internal sealed class DocumentDepartmentConfiguration : IEntityTypeConfiguration<DocumentDepartment>
{
    public void Configure(EntityTypeBuilder<DocumentDepartment> builder)
    {
        // Sammensatt nøkkel
        builder.HasKey(dd => new { dd.DocumentId, dd.DepartmentId });

        // Relasjon: DocumentDepartment → Document
        builder.HasOne(dd => dd.Department)
            .WithMany(d => d.DocumentDepartments)
            .HasForeignKey(dd => dd.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Relasjon: DocumentDepartment → Department
        builder.HasOne(dd => dd.Department)
            .WithMany()
            .HasForeignKey(dd => dd.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indeks for spørringer på avdeling
        builder.HasIndex(dd => dd.DepartmentId);
    }
}