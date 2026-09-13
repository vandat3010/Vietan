using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class TagHistoryConfigConfiguration : IEntityTypeConfiguration<TagHistoryConfig>
{
    public void Configure(EntityTypeBuilder<TagHistoryConfig> builder)
    {
        builder.ToTable("TagHistoryConfig", ScadaEntityConfiguration.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        builder.Ignore(e => e.CreatedAt);
        builder.Ignore(e => e.UpdatedAt);

        builder.Property(e => e.TagId).HasColumnName("TagId").IsRequired();
        builder.Property(e => e.HistoryProfileId).HasColumnName("HistoryProfileId").IsRequired();
        builder.Property(e => e.Deadband).HasColumnName("Deadband");
        builder.Property(e => e.Priority).HasColumnName("Priority").IsRequired();
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IsEnable).HasColumnName("IsEnable").IsRequired();

        builder.HasIndex(e => new { e.TagId, e.HistoryProfileId }).IsUnique();

        builder.HasOne(e => e.Tag)
            .WithMany(t => t.TagHistoryConfigs)
            .HasForeignKey(e => e.TagId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(e => e.HistoryProfile)
            .WithMany(p => p.TagHistoryConfigs)
            .HasForeignKey(e => e.HistoryProfileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
