using Backend.Api.Configuration;
using Backend.Api.Filters;
using Backend.Api.Realtime;
using Backend.Application.Options;
using Backend.Application.Realtime;
using Microsoft.AspNetCore.RateLimiting;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

namespace Backend.Api.Extensions;

/// <summary>
/// Registers everything specific to the presentation layer: MVC controllers +
/// the validation filter, CORS policy, and health checks. Keeps Program.cs
/// down to a short, readable list of "AddXxx()" calls.
/// </summary>
public static class ApiServiceExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddControllers(options =>
        {
            options.Filters.Add<ValidationFilter>();
        }).AddJsonOptions(options =>
        {
            options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });

        services.AddSignalR().AddJsonProtocol(options =>
        {
            options.PayloadSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.PayloadSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
        });
        services.AddSingleton<IScadaRealtimeBroadcaster, SignalRScadaRealtimeBroadcaster>();

        services.AddCorsPolicy(configuration);
        services.AddLoginRateLimiter(configuration);

        services.AddHealthChecks()
            .AddNpgSql(
                configuration.GetConnectionString("DefaultConnection")!,
                name: "postgresql",
                tags: ["db", "ready"]);

        return services;
    }

    /// <summary>
    /// Development: cho phép mọi origin localhost / 127.0.0.1 (mọi port) để FE
    /// Vite/Next/Angular không bị kẹt khi đổi port. Production: chỉ origins
    /// khai báo trong <c>Cors:AllowedOrigins</c>.
    /// </summary>
    private static void AddCorsPolicy(this IServiceCollection services, IConfiguration configuration)
    {
        var corsSettings = configuration.GetSection(CorsSettings.SectionName).Get<CorsSettings>()
            ?? new CorsSettings();

        var environment = configuration["ASPNETCORE_ENVIRONMENT"]
            ?? Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT")
            ?? "Production";

        var isDevelopment = string.Equals(environment, "Development", StringComparison.OrdinalIgnoreCase);

        services.AddCors(options =>
        {
            options.AddPolicy(CorsSettings.PolicyName, policy =>
            {
                if (isDevelopment)
                {
                    policy
                        .SetIsOriginAllowed(IsLocalFrontendOrigin)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("Token-Expired", "Content-Disposition", "X-Correlation-Id");
                    return;
                }

                if (corsSettings.AllowedOrigins.Length > 0)
                {
                    policy
                        .WithOrigins(corsSettings.AllowedOrigins)
                        .AllowAnyHeader()
                        .AllowAnyMethod()
                        .AllowCredentials()
                        .WithExposedHeaders("Token-Expired", "Content-Disposition", "X-Correlation-Id");
                }
                else
                {
                    // Production chưa cấu hình origin: fallback mở (giống trước),
                    // nhưng không kèm credentials (trình duyệt cấm AllowAnyOrigin + credentials).
                    policy.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
                }
            });
        });
    }

    /// <summary>
    /// BE 1.4 T2.4 — ASP.NET Core RateLimiter as defense-in-depth for the login
    /// endpoint ONLY (policy <c>auth-login</c>, opted in per-action). This does not
    /// replace the Redis failed-login counter; it caps raw request frequency per
    /// client IP so brute-force floods never reach the authentication logic.
    /// Deliberately NOT global — SignalR, realtime, PLC and monitoring APIs are
    /// never rate-limited.
    /// </summary>
    public const string LoginRateLimiterPolicy = "auth-login";

    private static void AddLoginRateLimiter(this IServiceCollection services, IConfiguration configuration)
    {
        var login = configuration.GetSection(LoginSecurityOptions.SectionName).Get<LoginSecurityOptions>()
            ?? new LoginSecurityOptions();

        var enabled = login.RateLimitEnabled;
        var permit = Math.Max(1, login.RateLimitPermitPerWindow);
        var window = TimeSpan.FromSeconds(Math.Max(1, login.RateLimitWindowSeconds));

        // Always register the limiter + policy so app.UseRateLimiter() and the
        // per-action [EnableRateLimiting] are always valid; when disabled the
        // policy resolves to a no-op partition (no throttling).
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(LoginRateLimiterPolicy, httpContext =>
            {
                var ip = httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
                if (!enabled)
                    return RateLimitPartition.GetNoLimiter(ip);

                return RateLimitPartition.GetFixedWindowLimiter(ip, _ => new FixedWindowRateLimiterOptions
                {
                    PermitLimit = permit,
                    Window = window,
                    QueueLimit = 0,
                    QueueProcessingOrder = QueueProcessingOrder.OldestFirst
                });
            });
        });
    }

    /// <summary>
    /// localhost và 127.0.0.1 là hai origin khác nhau với trình duyệt;
    /// Vite có thể chạy ở 5173/5174, Next ở 3000, Angular ở 4200.
    /// </summary>
    private static bool IsLocalFrontendOrigin(string origin)
    {
        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
            return false;

        if (uri.Scheme is not ("http" or "https"))
            return false;

        return uri.Host is "localhost" or "127.0.0.1" or "[::1]";
    }
}
