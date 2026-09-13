using Backend.Domain.Entities.History;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.History;

public class AlarmHistoryConfiguration : IEntityTypeConfiguration<AlarmHistory>
{
    public void Configure(EntityTypeBuilder<AlarmHistory> builder)
    {
        builder.ToTable("alarm_history", HistorySchema.Name);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        builder.Property(e => e.StationId).HasColumnName("StationId");
        builder.Property(e => e.PlcId).HasColumnName("PlcId");
        builder.Property(e => e.DeviceId).HasColumnName("DeviceId");
        builder.Property(e => e.TagId).HasColumnName("TagId");
        builder.Property(e => e.TagEventConfigId).HasColumnName("TagEventConfigId");
        builder.Property(e => e.EventTypeId).HasColumnName("EventTypeId");
        builder.Property(e => e.TriggerTypeId).HasColumnName("TriggerTypeId");
        builder.Property(e => e.DeviceName).HasColumnName("DeviceName");
        builder.Property(e => e.TagName).HasColumnName("TagName");
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.TroubleshootingGuide).HasColumnName("TroubleshootingGuide");
        builder.Property(e => e.Type).HasColumnName("Type");
        builder.Property(e => e.IsAcknowledged).HasColumnName("IsAcknowledged").IsRequired();
        builder.Property(e => e.DurationSeconds).HasColumnName("DurationSeconds");
        builder.Property(e => e.StartTime).HasColumnName("StartTime").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.EndTime).HasColumnName("EndTime").HasColumnType("timestamptz");
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamptz").IsRequired();

        builder.HasIndex(e => e.StartTime);
        builder.HasIndex(e => new { e.TagId, e.StartTime });
        builder.HasIndex(e => new { e.DeviceId, e.StartTime });
        builder.HasIndex(e => new { e.Type, e.StartTime });
    }
}
