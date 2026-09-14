using Backend.Application.Common;
using Backend.Application.Interfaces.Dapper;
using Backend.Application.Interfaces.Repositories;
using Backend.Application.Interfaces.Services;
using Backend.Application.Interfaces.Services.Scada;
using Backend.Application.Interfaces.UnitOfWork;
using Backend.Application.Options;
using Backend.Application.Realtime;
using Backend.Domain.Interfaces;
using Backend.Infrastructure.Common;
using Backend.Infrastructure.Scada;
using Backend.Infrastructure.Dapper.Context;
using Backend.Infrastructure.Dapper.Repository;
using Backend.Infrastructure.Identity;
using Backend.Infrastructure.Persistence.Auditing;
using Backend.Infrastructure.Persistence.Context;
using Backend.Infrastructure.Persistence.Repository;
using Backend.Infrastructure.Realtime;
using Backend.Infrastructure.Reporting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using UnitOfWorkImpl = Backend.Infrastructure.Persistence.UnitOfWork.UnitOfWork;

namespace Backend.Infrastructure;

/// <summary>
/// Single entry point for wiring the Infrastructure layer into the DI container.
/// Program.cs only ever calls <c>builder.Services.AddInfrastructure(configuration)</c> -
/// every concrete EF/Dapper/JWT/etc. type is registered here, behind the
/// Application-layer interfaces, so swapping an implementation never
/// requires touching the Api or Application projects.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        AddCommonServices(services, configuration);
        AddPersistence(services, configuration);
        AddDapper(services);
        AddReporting(services);
        AddIdentityServices(services, configuration);
        AddRealtime(services, configuration);

        return services;
    }

    /// <summary>
    /// Cross-cutting Infrastructure/Common services (ambient user, clock, storage,
    /// raw connections) that every other slice below depends on.
    /// </summary>
    private static void AddCommonServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddSingleton<IDateTimeProvider, DateTimeProvider>();
        services.AddSingleton<ISqlConnectionFactory, SqlConnectionFactory>();

        services.Configure<FileStorageSettings>(configuration.GetSection(FileStorageSettings.SectionName));
        services.AddSingleton<IFileStorageService, LocalFileStorageService>();

        // Swap this single line for a Redis-backed implementation and nothing in
        // Application/Domain changes - that is the whole point of ICacheService.
        services.AddMemoryCache();
        services.AddSingleton<ICacheService, MemoryCacheService>();

        // Default channel: structured logs. Replace with SignalR/SMTP/FCM per channel.
        services.AddScoped<INotificationService, LoggingNotificationService>();

        // In-process job queue. Hangfire/Quartz replace the service + processor.
        services.AddSingleton<BackgroundJobQueue>();
        services.AddSingleton<IBackgroundJobService, InMemoryBackgroundJobService>();
        services.AddHostedService<BackgroundJobProcessor>();
    }

    /// <summary>Reporting and bulk import/export - read-side, file-producing concerns.</summary>
    private static void AddReporting(IServiceCollection services)
    {
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IImportExportService, ExcelImportExportService>();
    }

    private static void AddPersistence(IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' was not found.");

        // EF Core is used ONLY for the write-side: Create/Update/Delete, migrations,
        // transactions and change tracking. See README "Why EF Core AND Dapper?".
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(connectionString, npgsql =>
            {
                npgsql.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName);
                npgsql.EnableRetryOnFailure(maxRetryCount: 3);
            }));

        // Open generics: any aggregate gets a repository for free, and only
        // ISoftDelete aggregates can resolve ISoftDeleteRepository<> (the generic
        // constraint makes an invalid closed type impossible to request).
        services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
        services.AddScoped(typeof(ISoftDeleteRepository<>), typeof(SoftDeleteRepository<>));
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWorkImpl>();
        services.AddScoped<IAuditService, AuditService>();

        // BE 3.1a — System Audit Log: fail-safe writer (isolated scope) + read side.
        services.AddSingleton<ISystemAuditService, SystemAuditService>();
        services.AddScoped<ISystemAuditLogQueryService, SystemAuditLogQueryService>();

        // SCADA metadata + history read APIs (one concrete type, many query interfaces).
        services.AddScoped<ScadaMetadataQueryService>();
        services.AddScoped<IStationQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<IPlcQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<IDeviceQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<ITagQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<IHistoryProfileQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<ITagHistoryConfigQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<ICommunicationConfigQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<IMqttConfigQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<IScadaUserQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());
        services.AddScoped<IAppSettingQueryService>(sp => sp.GetRequiredService<ScadaMetadataQueryService>());

        services.AddScoped<HistoryQueryService>();
        services.AddScoped<IHistorySampleQueryService>(sp => sp.GetRequiredService<HistoryQueryService>());
        services.AddScoped<IAlarmHistoryQueryService>(sp => sp.GetRequiredService<HistoryQueryService>());
        services.AddScoped<IScadaEventLogQueryService>(sp => sp.GetRequiredService<HistoryQueryService>());
        services.AddScoped<IUserActivityLogQueryService>(sp => sp.GetRequiredService<HistoryQueryService>());
        services.AddScoped<IScreenRealtimeQueryService, ScreenRealtimeQueryService>();
        services.AddHostedService<TagDefinitionExcelSeedHostedService>();
    }

    private static void AddDapper(IServiceCollection services)
    {
        // Dapper is used ONLY for the read-side: dashboards, reports, complex joins
        // and stored procedures. Never for Create/Update/Delete.
        services.AddSingleton<IDapperContext, DapperContext>();
        services.AddScoped<IDapperRepository, DapperRepository>();
        services.AddScoped<IUserReportQueries, UserReportQueries>();
    }

    private static void AddIdentityServices(IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<JwtSettings>()
            .Bind(configuration.GetSection(JwtSettings.SectionName))
            .Validate(o => o.Validate(out _), "Invalid 'Jwt' configuration (see JwtSettings.Validate). SigningKey must come from a secret store, not a placeholder.")
            .ValidateOnStart();
        services.Configure<PasswordHashingSettings>(configuration.GetSection(PasswordHashingSettings.SectionName));
        services.Configure<AuthSettings>(configuration.GetSection(AuthSettings.SectionName));
        services.Configure<ConcurrentSessionOptions>(configuration.GetSection(ConcurrentSessionOptions.SectionName));

        // Password complexity + expiry (distinct from Login lockout and session idle).
        services.AddOptions<PasswordPolicyOptions>()
            .Bind(configuration.GetSection(PasswordPolicyOptions.SectionName))
            .Validate(o =>
                    o.ExpireDays > 0
                    && o.WarnBeforeDays >= 0
                    && o.WarnBeforeDays < o.ExpireDays
                    && o.MinLength >= 1
                    && o.MaxLength >= o.MinLength
                    && o.ExpirationCheckIntervalMinutes > 0,
                "Invalid 'Password' configuration (ExpireDays, WarnBeforeDays, Min/MaxLength).")
            .ValidateOnStart();
        services.AddSingleton<IPasswordExpirationPolicy, PasswordExpirationPolicy>();

        // BE 1.4 — failed-login limiting / lockout thresholds. Validated on start
        // so an invalid config cannot silently disable brute-force protection.
        services.AddOptions<LoginSecurityOptions>()
            .Bind(configuration.GetSection(LoginSecurityOptions.SectionName))
            .Validate(o => o.Validate(out _), "Invalid 'Login' configuration (see LoginSecurityOptions.Validate).")
            .ValidateOnStart();

        // Argon2id is the only password hasher for both SCADA and IAM accounts.
        services.AddScoped<IPasswordHasher, Argon2idPasswordHasher>();
        services.AddScoped<IScadaTokenService, ScadaTokenService>();
        services.AddScoped<IAuthenticationService, AuthenticationService>();
        services.AddScoped<IConcurrentLicenseService, ConcurrentLicenseService>();
        services.AddScoped<ISystemLicenseAdminService, SystemLicenseAdminService>();
        services.AddScoped<IMapLayerService, MapLayerService>();
        services.AddScoped<IClientAuditService, ClientAuditService>();
        services.AddSingleton<IConcurrentSessionService, RedisConcurrentSessionService>();
        // Redis failed-login counter — reuses the shared IConnectionMultiplexer below.
        services.AddSingleton<ILoginAttemptService, RedisLoginAttemptService>();

        var redisConnection = configuration.GetConnectionString("Redis");
        if (string.IsNullOrWhiteSpace(redisConnection))
            throw new InvalidOperationException("Connection string 'Redis' was not found.");

        services.AddSingleton<IConnectionMultiplexer>(_ =>
        {
            var options = ConfigurationOptions.Parse(redisConnection);
            options.AbortOnConnectFail = false;
            return ConnectionMultiplexer.Connect(options);
        });

        // Legacy IAM token issuer (app.Users) — kept for IAuthService / UserService.
        services.AddScoped<ITokenService, JwtTokenService>();
    }

    private static void AddRealtime(IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<RealtimeOptions>(configuration.GetSection(RealtimeOptions.SectionName));

        var provider = configuration.GetSection(RealtimeOptions.SectionName)["Provider"] ?? "Fake";
        if (string.Equals(provider, "Redis", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<IRealtimeDataStore, RedisRealtimeDataStore>();
        else
            services.AddSingleton<IRealtimeDataStore, FakeRealtimeDataStore>();

        services.AddSingleton<PumpSimulationStateStore>();
        services.AddSingleton<IScadaRealtimeBroadcaster, NoopScadaRealtimeBroadcaster>();
        services.AddHostedService<FakeRealtimeSimulatorHostedService>();
    }
}
