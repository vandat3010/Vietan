using Backend.Domain.Entities.History;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations.History;

public class UserActivityLogConfiguration : IEntityTypeConfiguration<UserActivityLog>
{
    public void Configure(EntityTypeBuilder<UserActivityLog> builder)
    {
        builder.ToTable("UserActivityLogs", HistorySchema.Name);

        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).HasColumnName("Id").UseIdentityByDefaultColumn();
        builder.Property(e => e.CreatedAt).HasColumnName("CreatedAt").HasColumnType("timestamptz").IsRequired();
        builder.Property(e => e.UserId).HasColumnName("UserId");
        builder.Property(e => e.Username).HasColumnName("UserName");
        builder.Property(e => e.Role).HasColumnName("Role");
        builder.Property(e => e.ActionType).HasColumnName("ActionType").IsRequired();
        builder.Property(e => e.Module).HasColumnName("Module");
        builder.Property(e => e.Description).HasColumnName("Description");
        builder.Property(e => e.IpAddress).HasColumnName("IPAddress");
        builder.Property(e => e.Status).HasColumnName("Status");

        builder.HasIndex(e => e.CreatedAt);
        builder.HasIndex(e => e.Username);
        builder.HasIndex(e => e.ActionType);
        builder.HasIndex(e => e.UserId);
    }
}
