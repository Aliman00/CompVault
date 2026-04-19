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
        builder.HasOne(dd => dd.Document)
            .WithMany(d => d.DocumentDepartments)
            .HasForeignKey(dd => dd.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relasjon: DocumentDepartment → Department
        builder.HasOne(dd => dd.Department)
            .WithMany(d => d.DocumentDepartments)
            .HasForeignKey(dd => dd.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
        
        // Matcher query filters på Document og Department slik at avdelinger som er soft deleted ikke vil dukke opp
        // som målgrupper hos dokumenter lenger
        builder.HasQueryFilter(dd => dd.Document!.DeletedAt == null && dd.Department!.DeletedAt == null);
        
        // Indeks for spørringer på avdeling
        builder.HasIndex(dd => dd.DepartmentId);
    }
}