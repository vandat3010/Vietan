using System.Reflection;
using Backend.Application.Common;
using Backend.Domain.Common;
using Backend.Domain.Entities;
using Backend.Domain.Entities.History;
using Backend.Domain.Entities.Scada;
using Backend.Shared.Constants;
using Microsoft.EntityFrameworkCore;

namespace Backend.Infrastructure.Persistence.Context;

/// <summary>
/// The ONLY EF Core context in the solution. Owns the write-side model
/// (Create/Update/Delete, migrations, change tracking, transactions).
/// Read-heavy reporting queries never touch this class - see
/// Backend.Infrastructure.Dapper for that side of the CQRS-lite split.
/// </summary>
public class ApplicationDbContext : DbContext
{
    private readonly ICurrentUserService? _currentUserService;
    private readonly IDateTimeProvider? _dateTimeProvider;

    public ApplicationDbContext(
        DbContextOptions<ApplicationDbContext> options,
        ICurrentUserService? currentUserService = null,
        IDateTimeProvider? dateTimeProvider = null) : base(options)
    {
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    // --- IAM / app schema ---
    public DbSet<User> Users => Set<User>();
    public DbSet<Role> Roles => Set<Role>();
    public DbSet<Permission> Permissions => Set<Permission>();
    public DbSet<RolePermission> RolePermissions => Set<RolePermission>();
    public DbSet<UserRole> UserRoles => Set<UserRole>();
    public DbSet<RefreshToken> RefreshTokens => Set<RefreshToken>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    // --- SCADA metadata (schema scada) ---
    public DbSet<CommunicationConfig> CommunicationConfigs => Set<CommunicationConfig>();
    public DbSet<MqttConfig> MqttConfigs => Set<MqttConfig>();
    public DbSet<Station> Stations => Set<Station>();
    public DbSet<Plc> Plcs => Set<Plc>();
    public DbSet<Device> Devices => Set<Device>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<TagScreenMapping> TagScreenMappings => Set<TagScreenMapping>();
    public DbSet<HistoryProfile> HistoryProfiles => Set<HistoryProfile>();
    public DbSet<TagHistoryConfig> TagHistoryConfigs => Set<TagHistoryConfig>();
    public DbSet<ScadaUser> ScadaUsers => Set<ScadaUser>();
    public DbSet<ScadaRefreshToken> ScadaRefreshTokens => Set<ScadaRefreshToken>();
    public DbSet<ScadaPasswordResetToken> ScadaPasswordResetTokens => Set<ScadaPasswordResetToken>();
    public DbSet<AppSetting> AppSettings => Set<AppSetting>();
    public DbSet<SystemLicense> SystemLicenses => Set<SystemLicense>();
    public DbSet<MapLayer> MapLayers => Set<MapLayer>();
    public DbSet<SystemAuditLog> SystemAuditLogs => Set<SystemAuditLog>();
    public DbSet<DeviceType> DeviceTypes => Set<DeviceType>();
    public DbSet<EventType> EventTypes => Set<EventType>();
    public DbSet<TriggerType> TriggerTypes => Set<TriggerType>();
    public DbSet<TagEventConfig> TagEventConfigs => Set<TagEventConfig>();

    // --- Timescale history / logging (schema history) ---
    public DbSet<History1s> History1s => Set<History1s>();
    public DbSet<History30s> History30s => Set<History30s>();
    public DbSet<History1m> History1m => Set<History1m>();
    public DbSet<History30m> History30m => Set<History30m>();
    public DbSet<AlarmHistory> AlarmHistories => Set<AlarmHistory>();
    public DbSet<EventLog> EventLogs => Set<EventLog>();
    public DbSet<UserActivityLog> UserActivityLogs => Set<UserActivityLog>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
        modelBuilder.HasDefaultSchema("app");

        // Global query filter: every ISoftDelete entity is transparently excluded
        // from all LINQ queries once IsDeleted = true, without repositories having
        // to remember to add `.Where(x => !x.IsDeleted)` themselves.
        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        {
            if (typeof(ISoftDelete).IsAssignableFrom(entityType.ClrType))
            {
                var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                var property = System.Linq.Expressions.Expression.Property(parameter, nameof(ISoftDelete.IsDeleted));
                var condition = System.Linq.Expressions.Expression.Lambda(
                    System.Linq.Expressions.Expression.Equal(property, System.Linq.Expressions.Expression.Constant(false)),
                    parameter);

                modelBuilder.Entity(entityType.ClrType).HasQueryFilter(condition);
            }
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Central place where audit stamping, soft-delete conversion and domain-event
    /// dispatch happen for EVERY write, regardless of which repository triggered it.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        ApplyAuditInformation();
        ConvertHardDeletesToSoftDeletes();

        var domainEvents = CollectDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        // Dispatched in-process, after the transaction succeeds, so handlers never
        // see an event for data that ultimately failed to commit.
        await DispatchDomainEventsAsync(domainEvents, cancellationToken);

        return result;
    }

    private void ApplyAuditInformation()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var userId = CurrentUserIdentifier();

        // Timestamps live on BaseEntity (every entity gets them); the "who" fields
        // only exist on AuditableEntity, hence the two-step stamping below.
        // The generic base is used here because AuditableEntity<Guid> derives from
        // AggregateRoot<Guid>/BaseEntity<Guid> - not from the non-generic BaseEntity.
        foreach (var entry in ChangeTracker.Entries<BaseEntity<Guid>>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedDate = now;
                    if (entry.Entity is AuditableEntity<Guid> added) added.CreatedBy = userId;
                    break;
                case EntityState.Modified:
                    entry.Entity.ModifiedDate = now;
                    if (entry.Entity is AuditableEntity<Guid> modified) modified.ModifiedBy = userId;
                    break;
            }
        }
    }

    private void ConvertHardDeletesToSoftDeletes()
    {
        var now = _dateTimeProvider?.UtcNow ?? DateTime.UtcNow;
        var userId = CurrentUserIdentifier();

        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State != EntityState.Deleted) continue;

            entry.State = EntityState.Modified;
            entry.Entity.IsDeleted = true;
            entry.Entity.DeletedDate = now;
            entry.Entity.DeletedBy = userId;
        }
    }

    private string CurrentUserIdentifier() =>
        _currentUserService?.UserId?.ToString() ?? ApplicationConstants.SystemUserName;

    private List<IDomainEvent> CollectDomainEvents()
    {
        var aggregatesWithEvents = ChangeTracker.Entries<AggregateRoot<Guid>>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var domainEvents = aggregatesWithEvents
            .SelectMany(a => a.DomainEvents)
            .ToList();

        aggregatesWithEvents.ForEach(a => a.ClearDomainEvents());

        return domainEvents;
    }

    private static Task DispatchDomainEventsAsync(List<IDomainEvent> domainEvents, CancellationToken cancellationToken)
    {
        // Intentionally a no-op hook in this template: wire this into your preferred
        // in-process dispatcher (or publish onto Kafka/RabbitMQ, see README
        // "Extensibility") without the Application layer ever knowing which one.
        _ = domainEvents;
        _ = cancellationToken;
        return Task.CompletedTask;
    }
}
