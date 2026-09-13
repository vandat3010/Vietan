using Backend.Domain.Entities;
using Backend.Domain.Enums;
using Backend.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Backend.Infrastructure.Persistence.Configurations;

public class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable("Users");
        builder.HasKey(u => u.Id);
        builder.Ignore(u => u.DomainEvents);

        builder.Property(u => u.Email)
            .HasConversion(e => e.Value, v => Email.Create(v))
            .HasColumnName("Email")
            .HasMaxLength(256)
            .IsRequired();
        builder.HasIndex(u => u.Email).IsUnique();

        builder.OwnsOne(u => u.FullName, fn =>
        {
            fn.Property(f => f.FirstName).HasColumnName("FirstName").HasMaxLength(100).IsRequired();
            fn.Property(f => f.LastName).HasColumnName("LastName").HasMaxLength(100).IsRequired();
        });
        builder.Navigation(u => u.FullName).IsRequired();

        builder.Property(u => u.PasswordHash).HasMaxLength(500).IsRequired();
        builder.Property(u => u.PhoneNumber).HasMaxLength(20);
        builder.Property(u => u.Status).HasConversion<string>().HasMaxLength(30).HasDefaultValue(UserStatus.Active);
        builder.Property(u => u.FailedLoginAttempts).HasDefaultValue(0);

        builder.Property(u => u.CreatedBy).HasMaxLength(100);
        builder.Property(u => u.ModifiedBy).HasMaxLength(100);
        builder.Property(u => u.DeletedBy).HasMaxLength(100);

        builder.HasMany(u => u.UserRoles)
            .WithOne(ur => ur.User)
            .HasForeignKey(ur => ur.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.UserRoles).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.HasMany(u => u.RefreshTokens)
            .WithOne(rt => rt.User)
            .HasForeignKey(rt => rt.UserId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(u => u.RefreshTokens).UsePropertyAccessMode(PropertyAccessMode.Field);

        // Soft-delete query filter is applied generically for every ISoftDelete
        // entity in ApplicationDbContext.OnModelCreating - no need to repeat it here.
    }
}
