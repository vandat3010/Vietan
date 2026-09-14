using Backend.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class MapLayerConfiguration : IEntityTypeConfiguration<MapLayer>
{
    public void Configure(EntityTypeBuilder<MapLayer> builder)
    {
        builder.ToTable("map_layers", "app");

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Name).HasMaxLength(200).IsRequired();
        builder.Property(e => e.FileName).HasMaxLength(260).IsRequired();
        builder.Property(e => e.StoragePath).HasMaxLength(500).IsRequired();
        builder.Property(e => e.ContentType).HasMaxLength(120).IsRequired();
        builder.Property(e => e.Color).HasMaxLength(32).IsRequired();
        builder.Property(e => e.Opacity).HasPrecision(5, 4);
        builder.Property(e => e.Weight).HasPrecision(8, 2);

        builder.HasIndex(e => e.IsDeleted);
    }
}
