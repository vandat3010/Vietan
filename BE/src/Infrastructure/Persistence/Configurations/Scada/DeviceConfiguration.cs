using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class DeviceConfiguration : IEntityTypeConfiguration<Device>
{
    public void Configure(EntityTypeBuilder<Device> builder)
    {
        builder.ToTable("Device", ScadaEntityConfiguration.Schema);
        ScadaEntityConfiguration.ConfigureKeysAndTimestamps(builder);

        builder.Property(e => e.PlcId).HasColumnName("PlcId").IsRequired();
        // ERD live table has free-text DeviceType only; DeviceTypeId is optional extension.
        builder.Property(e => e.DeviceTypeId).HasColumnName("DeviceTypeId");
        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("DisplayName").IsRequired();
        builder.Property(e => e.DeviceType).HasColumnName("DeviceType").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");
        // ERD column name IsEnable; entity uses IsActive.
        builder.Property(e => e.IsActive).HasColumnName("IsEnable").IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.PlcId);

        builder.HasOne(e => e.Plc)
            .WithMany(p => p.Devices)
            .HasForeignKey(e => e.PlcId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.DeviceTypeNav)
            .WithMany(t => t.Devices)
            .HasForeignKey(e => e.DeviceTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(e => e.Tags)
            .WithOne(t => t.Device)
            .HasForeignKey(t => t.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
