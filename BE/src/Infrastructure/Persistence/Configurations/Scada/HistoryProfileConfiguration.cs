using Backend.Domain.Entities.Scada;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

public class HistoryProfileConfiguration : IEntityTypeConfiguration<HistoryProfile>
{
    public void Configure(EntityTypeBuilder<HistoryProfile> builder)
    {
        builder.ToTable("HistoryProfile", ScadaEntityConfiguration.Schema);
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        // ERD table has no CreatedTime/UpdatedTime.
        builder.Ignore(e => e.CreatedAt);
        builder.Ignore(e => e.UpdatedAt);

        builder.Property(e => e.Code).HasColumnName("Code").IsRequired();
        builder.Property(e => e.Name).HasColumnName("Name").IsRequired();
        builder.Property(e => e.IntervalSecond).HasColumnName("IntervalSecond").IsRequired();
        builder.Property(e => e.RetentionDay).HasColumnName("RetentionDay").IsRequired();
        builder.Property(e => e.CompressionDay).HasColumnName("CompressionDay");
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IsEnable).HasColumnName("IsEnable").IsRequired();
    }
}
