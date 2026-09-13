using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class MqttConfigConfiguration : IEntityTypeConfiguration<MqttConfig>
{
    public void Configure(EntityTypeBuilder<MqttConfig> builder)
    {
        builder.ToTable("mqtt_config", ScadaEntityConfiguration.AppSchema);
        ScadaEntityConfiguration.ConfigureAppKeysAndTimestamps(builder);

        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Broker).HasColumnName("broker").HasMaxLength(255).IsRequired();
        builder.Property(e => e.Port).HasColumnName("port").IsRequired();
        builder.Property(e => e.Username).HasColumnName("username").HasMaxLength(100);
        builder.Property(e => e.Password).HasColumnName("password").HasMaxLength(255);
        builder.Property(e => e.ClientId).HasColumnName("client_id").HasMaxLength(100);
        builder.Property(e => e.TopicPublish).HasColumnName("topic_publish").HasMaxLength(255);
        builder.Property(e => e.TopicSubscribe).HasColumnName("topic_subscribe").HasMaxLength(255);
        builder.Property(e => e.KeepAlive).HasColumnName("keep_alive").IsRequired();
        builder.Property(e => e.Qos).HasColumnName("qos").IsRequired();
        builder.Property(e => e.Retain).HasColumnName("retain").IsRequired();
        builder.Property(e => e.UseTls).HasColumnName("use_tls").IsRequired();
        builder.Property(e => e.IsEnable).HasColumnName("is_enable").IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.HasIndex(e => e.Code).IsUnique();
    }
}
