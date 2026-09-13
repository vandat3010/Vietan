using Backend.Api.Configuration;
using Backend.Api.Extensions;
using Backend.Api.Middlewares;
using Backend.Api.Swagger;
using Backend.Application;
using Backend.Infrastructure;
using Backend.Infrastructure.Logging;
using Backend.Infrastructure.Persistence.Context;
using Microsoft.EntityFrameworkCore;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

// Serilog replaces the default provider as early as possible so that even
// host-startup failures are captured with the same sinks/format as the app.
Log.Logger = SerilogConfiguration.CreateLogger(builder.Configuration, builder.Environment.EnvironmentName);
builder.Host.UseSerilog();

// ---------------------------------------------------------------------------
// Dependency Injection: each layer owns and exposes exactly one entry point.
// Program.cs never registers an Application/Infrastructure type directly.
// ---------------------------------------------------------------------------
// Fail fast on DI mistakes (missing registration, or a singleton capturing a
// scoped service such as DbContext) at startup instead of on the first request
// that happens to hit the broken path. Development only: both checks walk the
// whole graph and cost startup time.
builder.Host.UseDefaultServiceProvider((context, options) =>
{
    options.ValidateScopes = context.HostingEnvironment.IsDevelopment();
    options.ValidateOnBuild = context.HostingEnvironment.IsDevelopment();
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddApiServices(builder.Configuration);

builder.Services.AddJwtAuthentication(builder.Configuration);

builder.Services.AddSwaggerDocumentation();

var app = builder.Build();

// ---------------------------------------------------------------------------
// HTTP request pipeline - order matters here.
// CORS must run BEFORE exception handling so error responses (4xx/5xx) still
// carry Access-Control-Allow-* headers; otherwise the browser reports a CORS
// failure and hides the real API error from the frontend.
// ---------------------------------------------------------------------------
app.UseCors(CorsSettings.PolicyName);

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<PerformanceMiddleware>();

app.UseSerilogRequestLogging();

// BE 1.4 T2.4 — rate limiter (login endpoint only, via [EnableRateLimiting]).
app.UseRateLimiter();

if (app.Environment.IsDevelopment())
{
    app.UseSwaggerDocumentation();
}
else
{
    // HTTPS redirect in Development often breaks FE preflight (OPTIONS → 307
    // without CORS headers) when the SPA calls http://localhost:5140.
    app.UseHttpsRedirection();
}

app.UseAuthentication();

// BE 1.3a — must run AFTER authentication (needs HttpContext.User) and before
// business endpoints.
app.UseMiddleware<Backend.Api.Middlewares.MustChangePasswordMiddleware>();

app.UseAuthorization();

app.MapControllers();
app.MapHub<Backend.Api.Realtime.ScadaRealtimeHub>("/hubs/scada");
app.MapHealthChecks("/health");

// Legacy EF migrations target the old scada/history schemas.
// scada_tlhn (ERD public) is bootstrapped via scripts/bootstrap_scada_tlhn.sql.
if (app.Environment.IsDevelopment()
    && app.Configuration.GetValue("Database:ApplyEfMigrations", true))
{
    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await dbContext.Database.MigrateAsync();
}

try
{
    Log.Information("Starting Backend.Api");
    await app.RunAsync();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Backend.Api terminated unexpectedly");
}
finally
{
    await Log.CloseAndFlushAsync();
}

/// <summary>Exposed for WebApplicationFactory-based integration tests.</summary>
public partial class Program;
