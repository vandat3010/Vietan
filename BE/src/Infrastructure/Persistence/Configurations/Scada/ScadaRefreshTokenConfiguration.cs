using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class ScadaRefreshTokenConfiguration : IEntityTypeConfiguration<ScadaRefreshToken>
{
    public void Configure(EntityTypeBuilder<ScadaRefreshToken> builder)
    {
        builder.ToTable("refresh_tokens", ScadaEntityConfiguration.AppSchema);
        ScadaEntityConfiguration.ConfigureAppKeysAndTimestamps(builder);

        builder.Property(e => e.UserId).HasColumnName("user_id").IsRequired();
        builder.Property(e => e.TokenHash).HasColumnName("token_hash").HasMaxLength(128).IsRequired();
        builder.Property(e => e.ExpiresAt).HasColumnName("expires_at").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.RevokedAt).HasColumnName("revoked_at").HasColumnType("timestamptz");
        builder.Property(e => e.RevokedByIp).HasColumnName("revoked_by_ip").HasMaxLength(50);
        builder.Property(e => e.CreatedByIp).HasColumnName("created_by_ip").HasMaxLength(50);
        builder.Property(e => e.ReplacedByTokenId).HasColumnName("replaced_by_token_id");
        builder.Property(e => e.SessionId).HasColumnName("session_id").HasMaxLength(64);

        builder.HasIndex(e => e.TokenHash).IsUnique();
        builder.HasIndex(e => e.UserId);
        builder.HasIndex(e => e.SessionId);

        builder.HasOne(e => e.User)
            .WithMany()
            .HasForeignKey(e => e.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Ignore(e => e.IsActive);
    }
}
