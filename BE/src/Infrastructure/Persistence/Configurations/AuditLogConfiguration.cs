using Backend.Domain.Entities;
using Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
    public void Configure(EntityTypeBuilder<AuditLog> builder)
    {
        builder.ToTable("AuditLogs");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.EntityName).IsRequired().HasMaxLength(ValidationConstants.NameMaxLength);
        builder.Property(a => a.EntityId).HasMaxLength(ValidationConstants.NameMaxLength);
        builder.Property(a => a.Action).HasConversion<string>().HasMaxLength(50).IsRequired();
        builder.Property(a => a.UserId).HasMaxLength(ValidationConstants.AuditUserMaxLength);
        builder.Property(a => a.UserName).HasMaxLength(ValidationConstants.AuditUserMaxLength);
        builder.Property(a => a.IpAddress).HasMaxLength(ValidationConstants.IpAddressMaxLength);
        builder.Property(a => a.CorrelationId).HasMaxLength(ValidationConstants.NameMaxLength);

        // The two access patterns an audit screen actually uses: "history of this
        // record" and "everything that happened in this time window".
        builder.HasIndex(a => new { a.EntityName, a.EntityId });
        builder.HasIndex(a => a.CreatedDate);
    }
}
