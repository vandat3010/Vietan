using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Backend.Domain.Entities.Scada;

namespace Backend.Infrastructure.Persistence.Configurations.Scada;

internal static class ScadaEntityConfiguration
{
    /// <summary>ERD live database (<c>scada_tlhn</c>) — metadata tables live in <c>public</c>.</summary>
    internal const string Schema = "public";

    /// <summary>App-only extensions beyond ERD (auth tokens, system audit, settings).</summary>
    internal const string AppSchema = "app";

    /// <summary>Maps Id / CreatedTime / UpdatedTime used by ERD PascalCase tables.</summary>
    internal static void ConfigureKeysAndTimestamps<T>(EntityTypeBuilder<T> builder)
        where T : ScadaEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("Id")
            .UseIdentityByDefaultColumn();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("CreatedTime")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("UpdatedTime")
            .HasColumnType("timestamptz")
            .IsRequired();
    }

    /// <summary>App-schema tables keep snake_case timestamps.</summary>
    internal static void ConfigureAppKeysAndTimestamps<T>(EntityTypeBuilder<T> builder)
        where T : ScadaEntity
    {
        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id)
            .HasColumnName("id")
            .UseIdentityByDefaultColumn();

        builder.Property(e => e.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamptz")
            .IsRequired();

        builder.Property(e => e.UpdatedAt)
            .HasColumnName("updated_at")
            .HasColumnType("timestamptz")
            .IsRequired();
    }
}
