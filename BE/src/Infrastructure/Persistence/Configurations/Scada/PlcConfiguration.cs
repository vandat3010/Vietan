using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class PlcConfiguration : IEntityTypeConfiguration<Plc>
{
    public void Configure(EntityTypeBuilder<Plc> builder)
    {
        builder.ToTable("PLC", ScadaEntityConfiguration.Schema);
        ScadaEntityConfiguration.ConfigureKeysAndTimestamps(builder);

        builder.Property(e => e.StationId).HasColumnName("StationId").IsRequired();
        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.PlcType).HasColumnName("PlcType").IsRequired();
        builder.Property(e => e.IpAddress).HasColumnName("IPAddress").IsRequired();
        builder.Property(e => e.Rack).HasColumnName("Rack").IsRequired();
        builder.Property(e => e.Slot).HasColumnName("Slot").IsRequired();
        builder.Property(e => e.Port).HasColumnName("Port").IsRequired();
        builder.Property(e => e.PollingInterval).HasColumnName("PollingInterval").IsRequired();
        builder.Property(e => e.ReconnectInterval).HasColumnName("ReconnectInterval").IsRequired();
        builder.Property(e => e.Timeout).HasColumnName("Timeout").IsRequired();
        builder.Property(e => e.MaxConnection).HasColumnName("MaxConnection").IsRequired();
        builder.Property(e => e.IsEnable).HasColumnName("IsEnable").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");

        builder.HasIndex(e => e.Code).IsUnique();
        builder.HasIndex(e => e.StationId);

        builder.HasOne(e => e.Station)
            .WithMany(s => s.Plcs)
            .HasForeignKey(e => e.StationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Devices)
            .WithOne(d => d.Plc)
            .HasForeignKey(d => d.PlcId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(e => e.Tags)
            .WithOne(t => t.Plc)
            .HasForeignKey(t => t.PlcId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
