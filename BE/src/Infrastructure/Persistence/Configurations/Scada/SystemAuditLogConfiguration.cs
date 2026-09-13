using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class SystemAuditLogConfiguration : IEntityTypeConfiguration<SystemAuditLog>
{
    public void Configure(EntityTypeBuilder<SystemAuditLog> builder)
    {
        builder.ToTable("system_audit_logs", ScadaEntityConfiguration.AppSchema);
        ScadaEntityConfiguration.ConfigureAppKeysAndTimestamps(builder);

        builder.Property(e => e.Action).HasColumnName("action").HasMaxLength(100).IsRequired();
        builder.Property(e => e.EventType)
            .HasColumnName("event_type").HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(e => e.Status)
            .HasColumnName("status").HasConversion<string>().HasMaxLength(20).IsRequired();
        builder.Property(e => e.UserId).HasColumnName("user_id");
        builder.Property(e => e.UserName).HasColumnName("user_name").HasMaxLength(100);
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.Module).HasColumnName("module").HasMaxLength(100);
        builder.Property(e => e.Endpoint).HasColumnName("endpoint").HasMaxLength(300);
        builder.Property(e => e.HttpMethod).HasColumnName("http_method").HasMaxLength(10);
        builder.Property(e => e.HttpStatusCode).HasColumnName("http_status_code");
        builder.Property(e => e.IpAddress).HasColumnName("ip_address").HasMaxLength(64);
        builder.Property(e => e.UserAgent).HasColumnName("user_agent").HasMaxLength(512);
        builder.Property(e => e.EntityType).HasColumnName("entity_type").HasMaxLength(100);
        builder.Property(e => e.EntityId).HasColumnName("entity_id").HasMaxLength(100);
        builder.Property(e => e.CorrelationId).HasColumnName("correlation_id").HasMaxLength(100);
        builder.Property(e => e.AdditionalData).HasColumnName("additional_data").HasColumnType("jsonb");

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.EventType);
        builder.HasIndex(e => e.Action);
        builder.HasIndex(e => e.Status);
        builder.HasIndex(e => e.UserName);
        builder.HasIndex(e => e.CorrelationId);
    }
}
