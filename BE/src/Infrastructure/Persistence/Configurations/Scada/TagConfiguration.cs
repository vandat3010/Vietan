using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class TagConfiguration : IEntityTypeConfiguration<Tag>
{
    public void Configure(EntityTypeBuilder<Tag> builder)
    {
        builder.ToTable("Tag", ScadaEntityConfiguration.Schema);
        ScadaEntityConfiguration.ConfigureKeysAndTimestamps(builder);

        builder.Property(e => e.PlcId).HasColumnName("PlcId").IsRequired();
        builder.Property(e => e.DeviceId).HasColumnName("DeviceId").IsRequired();
        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.TagName).HasColumnName("Tag").IsRequired();
        builder.Property(e => e.DisplayName).HasColumnName("DisplayName");
        builder.Property(e => e.Address).HasColumnName("Address").IsRequired();
        builder.Property(e => e.DataType).HasColumnName("DataType").IsRequired();
        builder.Property(e => e.Unit).HasColumnName("Unit");
        builder.Property(e => e.Scale).HasColumnName("Scale");
        builder.Property(e => e.OffsetValue).HasColumnName("Offset");
        builder.Property(e => e.ReadOnly).HasColumnName("ReadOnly").IsRequired();
        builder.Property(e => e.WriteEnable).HasColumnName("WriteEnable").IsRequired();
        builder.Property(e => e.EnableRealtime).HasColumnName("EnableRealtime").IsRequired();
        builder.Property(e => e.EnableAlarm).HasColumnName("EnableAlarm").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");
        // Optional extension column — created by bootstrap if missing.
        builder.Property(e => e.IsActive).HasColumnName("IsActive").HasDefaultValue(true).IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.PlcId);
        builder.HasIndex(e => e.DeviceId);

        builder.HasOne(e => e.Plc)
            .WithMany(p => p.Tags)
            .HasForeignKey(e => e.PlcId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(e => e.Device)
            .WithMany(d => d.Tags)
            .HasForeignKey(e => e.DeviceId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.TagHistoryConfigs)
            .WithOne(c => c.Tag)
            .HasForeignKey(c => c.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(e => e.ScreenMappings)
            .WithOne(m => m.Tag)
            .HasForeignKey(m => m.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
