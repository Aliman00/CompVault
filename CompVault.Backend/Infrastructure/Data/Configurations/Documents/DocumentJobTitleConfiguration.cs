using CompVault.Backend.Domain.Entities.Documents;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Documents;

/// <summary>
/// EF Core-konfigurasjon for DocumentJobTitle-koblingstabellen.
/// </summary>
internal sealed class DocumentJobTitleConfiguration : IEntityTypeConfiguration<DocumentJobTitle>
{
    public void Configure(EntityTypeBuilder<DocumentJobTitle> builder)
    {
        // Sammensatt nøkkel
        builder.HasKey(dj => new { dj.DocumentId, dj.JobTitleId });

        // Relasjon: DocumentJobTitle → Document
        builder.HasOne(dj => dj.Document)
            .WithMany(d => d.DocumentJobTitles)
            .HasForeignKey(dj => dj.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        // Relasjon: DocumentJobTitle → JobTitle
        builder.HasOne(dj => dj.JobTitle)
            .WithMany(j => j.DocumentJobTitles)
            .HasForeignKey(dj => dj.JobTitleId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indeks for spørringer på jobbtittel
        builder.HasIndex(dj => dj.JobTitleId);
    }
}