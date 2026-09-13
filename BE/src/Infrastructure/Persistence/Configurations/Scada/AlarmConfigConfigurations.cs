using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class DeviceTypeConfiguration : IEntityTypeConfiguration<DeviceType>
{
    public void Configure(EntityTypeBuilder<DeviceType> builder)
    {
        builder.ToTable("DeviceType", ScadaEntityConfiguration.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();
        builder.Property(e => e.SortOrder).HasColumnName("SortOrder").IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> builder)
    {
        builder.ToTable("EventType", ScadaEntityConfiguration.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IsEnable).HasColumnName("IsEnable").IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

public class TriggerTypeConfiguration : IEntityTypeConfiguration<TriggerType>
{
    public void Configure(EntityTypeBuilder<TriggerType> builder)
    {
        builder.ToTable("TriggerType", ScadaEntityConfiguration.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IsEnable).HasColumnName("IsEnable").IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}

public class TagEventConfigConfiguration : IEntityTypeConfiguration<TagEventConfig>
{
    public void Configure(EntityTypeBuilder<TagEventConfig> builder)
    {
        builder.ToTable("TagEventConfig", ScadaEntityConfiguration.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        // ERD uses CreatedAt/UpdatedAt (not CreatedTime).
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UpdatedAt).HasColumnName("UpdatedAt").HasColumnType("timestamptz").IsRequired();

        builder.Property(e => e.TagId).HasColumnName("TagId").IsRequired();
        builder.Property(e => e.EventTypeId).HasColumnName("EventTypeId").IsRequired();
        builder.Property(e => e.TriggerTypeId).HasColumnName("TriggerTypeId").IsRequired();
        builder.Property(e => e.TriggerValue).HasColumnName("TriggerValue");
        builder.Property(e => e.Deadband).HasColumnName("Deadband");
        builder.Property(e => e.Message).HasColumnName("Message");
        builder.Property(e => e.TroubleshootingGuide).HasColumnName("TroubleshootingGuide");
        builder.Property(e => e.Severity).HasColumnName("Severity").IsRequired();
        builder.Property(e => e.IsEnable).HasColumnName("IsEnabled").IsRequired();
        builder.Property(e => e.FunctionDescription).HasColumnName("FunctionDescription");

        builder.HasOne(e => e.Tag).WithMany(t => t.TagEventConfigs).HasForeignKey(e => e.TagId);
        builder.HasOne(e => e.EventType).WithMany(t => t.TagEventConfigs).HasForeignKey(e => e.EventTypeId);
        builder.HasOne(e => e.TriggerType).WithMany(t => t.TagEventConfigs).HasForeignKey(e => e.TriggerTypeId);

        builder.HasIndex(e => e.TagId);
        builder.HasIndex(e => e.EventTypeId);
        builder.HasIndex(e => e.TriggerTypeId);
    }
}
