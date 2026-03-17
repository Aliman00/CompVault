using CompVault.Backend.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Auth;

internal sealed class OtpCodeConfiguration : IEntityTypeConfiguration<OtpCode>
{
    public void Configure(EntityTypeBuilder<OtpCode> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.UserId).IsRequired();
        builder.Property(o => o.Code).HasMaxLength(64).IsRequired(); // Hasher koden
        builder.Property(o => o.ExpiresAt).IsRequired();
        builder.Property(o => o.CreatedAt).IsRequired();
        builder.Property(o => o.IsUsed).IsRequired();
        builder.Property(o => o.FailedAttempts).IsRequired();

        // Matcher query filteret på ApplicationUser slik at soft-slettede brukere
        // ikke forårsaker uventede resultater i joins
        builder.HasQueryFilter(o => o.User.DeletedAt == null);

        // Relasjon mellom User og OtpCodes - En OtpCode hører til en User, og en User kan ha mange OtpCodes
        builder.HasOne(o => o.User)
            .WithMany(u => u.OtpCodes)
            .HasForeignKey(o => o.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => o.UserId);
    }
}

