using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class CommunicationConfigConfiguration : IEntityTypeConfiguration<CommunicationConfig>
{
    public void Configure(EntityTypeBuilder<CommunicationConfig> builder)
    {
        builder.ToTable("communication_config", ScadaEntityConfiguration.AppSchema);
        ScadaEntityConfiguration.ConfigureAppKeysAndTimestamps(builder);

        builder.Property(e => e.Code).HasColumnName("code").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Name).HasColumnName("name").HasMaxLength(100).IsRequired();
        builder.Property(e => e.Protocol).HasColumnName("protocol").HasMaxLength(50).IsRequired();
        builder.Property(e => e.Description).HasColumnName("description").HasColumnType("text");
        builder.Property(e => e.IsEnable).HasColumnName("is_enable").IsRequired();
        builder.HasIndex(e => e.Code).IsUnique();
    }
}
