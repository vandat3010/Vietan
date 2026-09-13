using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class StationConfiguration : IEntityTypeConfiguration<Station>
{
    public void Configure(EntityTypeBuilder<Station> builder)
    {
        builder.ToTable("Station", ScadaEntityConfiguration.Schema);
        ScadaEntityConfiguration.ConfigureKeysAndTimestamps(builder);

        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.Address).HasColumnName("Address");
        builder.Property(e => e.Latitude).HasColumnName("Latitude");
        builder.Property(e => e.Longitude).HasColumnName("Longitude");
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IsActive).HasColumnName("IsActive").IsRequired();

        builder.HasIndex(e => e.Code).IsUnique();

        builder.HasMany(e => e.Plcs)
            .WithOne(p => p.Station)
            .HasForeignKey(p => p.StationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
