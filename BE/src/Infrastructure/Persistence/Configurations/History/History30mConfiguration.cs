using Backend.Domain.Entities.History;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.History;

public class History30mConfiguration : IEntityTypeConfiguration<History30m>
{
    public void Configure(EntityTypeBuilder<History30m> builder)
    {
        builder.ToTable("history_30m", HistorySchema.Name);
        builder.HasKey(e => new { e.Time, e.TagId });
        builder.Property(e => e.Time).HasColumnName("Time").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.TagId).HasColumnName("TagId").IsRequired();
        builder.Property(e => e.Value).HasColumnName("Value").IsRequired();
        builder.HasIndex(e => new { e.TagId, e.Time });
    }
}
