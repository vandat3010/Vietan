using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class ScadaUserConfiguration : IEntityTypeConfiguration<ScadaUser>
{
    public void Configure(EntityTypeBuilder<ScadaUser> builder)
    {
        // ERD Users table in public (scada_tlhn). IAM Users stay in schema app.
        builder.ToTable("Users", ScadaEntityConfiguration.Schema);
        ScadaEntityConfiguration.ConfigureKeysAndTimestamps(builder);

        builder.Property(e => e.Username).HasColumnName("Username").IsRequired();
        builder.Property(e => e.PasswordHash).HasColumnName("PasswordHash").IsRequired();
        builder.Property(e => e.FullName).HasColumnName("FullName").IsRequired();
        builder.Property(e => e.Email).HasColumnName("Email");
        builder.Property(e => e.Role).HasColumnName("Role").IsRequired();
        builder.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();

        // Extensions beyond ERD (bootstrap ALTER TABLE).
        builder.Property(e => e.LastLoginAt).HasColumnName("LastLoginAt").HasColumnType("timestamptz");
        builder.Property(e => e.Unit).HasColumnName("Unit");
        builder.Property(e => e.Level).HasColumnName("Level");
        builder.Property(e => e.Department).HasColumnName("Department");
        builder.Property(e => e.Position).HasColumnName("Position");
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.CreatedBy).HasColumnName("CreatedBy");
        builder.Property(e => e.UpdatedBy).HasColumnName("UpdatedBy");
        builder.Property(e => e.MustChangePassword).HasColumnName("MustChangePassword").HasDefaultValue(false).IsRequired();
        builder.Property(e => e.PasswordUpdatedAt).HasColumnName("PasswordUpdatedAt").HasColumnType("timestamptz");
        builder.Property(e => e.FailedLoginCount).HasColumnName("FailedLoginCount").HasDefaultValue(0).IsRequired();
        builder.Property(e => e.LockoutUntil).HasColumnName("LockoutUntil").HasColumnType("timestamptz");

        builder.HasIndex(e => e.Username).IsUnique();
    }
}
