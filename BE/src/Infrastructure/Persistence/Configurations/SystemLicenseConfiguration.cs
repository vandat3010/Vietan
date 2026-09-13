using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class SystemLicenseConfiguration : IEntityTypeConfiguration<SystemLicense>
{
    public void Configure(EntityTypeBuilder<SystemLicense> builder)
    {
        builder.ToTable("system_licenses", "app");

        builder.HasKey(e => e.Id);

        builder.Property(e => e.LicenseKey)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(e => e.MaxConcurrentUsers)
            .IsRequired();

        builder.Property(e => e.IsEnabled)
            .IsRequired();

        builder.Property(e => e.Description)
            .HasMaxLength(500);

        builder.Property(e => e.ValidFrom).HasColumnType("timestamptz");
        builder.Property(e => e.ValidTo).HasColumnType("timestamptz");

        // Bootstrap table uses CreatedAt/ModifiedAt/DeletedAt naming.
        builder.Property(e => e.CreatedDate).HasColumnName("CreatedAt");
        builder.Property(e => e.ModifiedDate).HasColumnName("ModifiedAt");
        builder.Property(e => e.DeletedDate).HasColumnName("DeletedAt");

        builder.HasIndex(e => e.IsEnabled);
        builder.HasIndex(e => e.ValidTo);
        builder.HasIndex(e => e.LicenseKey);
    }
}
