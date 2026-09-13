using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class AppSettingConfiguration : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.ToTable("app_settings", ScadaEntityConfiguration.AppSchema);
        ScadaEntityConfiguration.ConfigureAppKeysAndTimestamps(builder);

        builder.Property(e => e.SettingKey).HasColumnName("setting_key").HasMaxLength(100).IsRequired();
        builder.Property(e => e.SettingValue).HasColumnName("setting_value").HasColumnType("text");
        builder.Property(e => e.DataType).HasColumnName("data_type").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.IsEnable).HasColumnName("is_enable").IsRequired();
        builder.HasIndex(e => e.SettingKey).IsUnique();
    }
}
