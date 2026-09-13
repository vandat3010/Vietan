using Backend.Domain.Entities.History;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.History;

public class History1mConfiguration : IEntityTypeConfiguration<History1m>
{
    public void Configure(EntityTypeBuilder<History1m> builder)
    {
        builder.ToTable("history_1m", HistorySchema.Name);
        builder.HasKey(e => new { e.Time, e.TagId });
        builder.Property(e => e.Time).HasColumnName("Time").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.TagId).HasColumnName("TagId").IsRequired();
        builder.Property(e => e.Value).HasColumnName("Value").IsRequired();
        builder.HasIndex(e => new { e.TagId, e.Time });
    }
}
