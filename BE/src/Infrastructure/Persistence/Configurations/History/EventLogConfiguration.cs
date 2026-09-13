using Backend.Domain.Entities.History;
using Backend.Infrastructure.Persistence.Configurations.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.History;

/// <summary>
/// App event log (Level/Module/...). Distinct from ERD <c>public.event_log</c>
/// (EventType/Message/Details) which has a different shape.
/// </summary>
public class EventLogConfiguration : IEntityTypeConfiguration<EventLog>
{
    public void Configure(EntityTypeBuilder<EventLog> builder)
    {
        builder.ToTable("event_logs", ScadaEntityConfiguration.AppSchema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("id").UseIdentityByDefaultColumn();
        builder.Property(e => e.Time).HasColumnName("time").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.Level).HasColumnName("level").HasMaxLength(30).IsRequired();
        builder.Property(e => e.Module).HasColumnName("module").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Message).HasColumnName("message").HasColumnType("text").IsRequired();
        builder.Property(e => e.Exception).HasColumnName("exception").HasColumnType("text");
        builder.Property(e => e.Machine).HasColumnName("machine").HasMaxLength(100);
        builder.HasIndex(e => e.Time);
        builder.HasIndex(e => new { e.Module, e.Time });
    }
}
