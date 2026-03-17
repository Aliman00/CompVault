using CompVault.Backend.Domain.Entities.Auth;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CompVault.Backend.Infrastructure.Data.Configurations.Auth;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.HasKey(r => r.Id);

        builder.Property(r => r.UserId).IsRequired();
        builder.Property(r => r.Token).HasMaxLength(128).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.IsRevoked).IsRequired();

        // Soft-delete filter — ikke vis tokens tilhørende slettede brukere
        builder.Property(r => r.CreatedAt)
            .HasDefaultValueSql("NOW()");

        // Relasjon: en bruker kan ha mange refresh tokens
        builder.HasOne(r => r.User)
            .WithMany(u => u.RefreshTokens)
            .HasForeignKey(r => r.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Index på Token for rask oppslag ved refresh
        builder.HasIndex(r => r.Token).IsUnique();
        // Index på UserId for rask oppslag ved revoke-alle-for-bruker
        builder.HasIndex(r => r.UserId);
    }
}
