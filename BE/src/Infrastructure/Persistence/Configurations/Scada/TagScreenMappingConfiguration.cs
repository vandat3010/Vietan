using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class TagScreenMappingConfiguration : IEntityTypeConfiguration<TagScreenMapping>
{
    public void Configure(EntityTypeBuilder<TagScreenMapping> builder)
    {
        builder.ToTable("tag_screen_mapping", ScadaEntityConfiguration.AppSchema);
        ScadaEntityConfiguration.ConfigureAppKeysAndTimestamps(builder);

        builder.Property(e => e.TagId).HasColumnName("tag_id").IsRequired();
        builder.Property(e => e.ScreenType).HasColumnName("screen_type").HasConversion<int>().IsRequired();
        builder.Property(e => e.IsRealtime).HasColumnName("is_realtime").IsRequired();
        builder.Property(e => e.MappingLabel).HasColumnName("mapping_label").HasMaxLength(200);

        builder.HasIndex(e => new { e.TagId, e.ScreenType }).IsUnique();
        builder.HasIndex(e => e.ScreenType);
        builder.HasIndex(e => new { e.ScreenType, e.IsRealtime });

        builder.HasOne(e => e.Tag)
            .WithMany(t => t.ScreenMappings)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
