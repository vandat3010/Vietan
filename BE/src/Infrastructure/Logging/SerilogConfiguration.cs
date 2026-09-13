using Microsoft.Extensions.Configuration;
using Serilog;
using Serilog.Events;

namespace Backend.Infrastructure.Logging;

/// <summary>
/// Builds the application-wide Serilog logger. Kept in Infrastructure (rather than
/// inline in Program.cs) so the exact sinks/enrichers used are a swappable
/// implementation detail - e.g. adding a Seq or Elasticsearch sink later only
/// touches this file.
/// </summary>
public static class SerilogConfiguration
{
    public static Serilog.ILogger CreateLogger(IConfiguration configuration, string environmentName)
    {
        return new LoggerConfiguration()
            .Enrich.WithProperty("Environment", environmentName)
            .MinimumLevel.Information()
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
            .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .Enrich.WithThreadId()
            .Enrich.WithEnvironmentName()
            .Enrich.WithProperty("Application", "Backend.Api")
            .WriteTo.Console(
                outputTemplate:
                "[{Timestamp:HH:mm:ss} {Level:u3}] ({CorrelationId}) {Message:lj} {Properties:j}{NewLine}{Exception}")
            .WriteTo.File(
                path: "Logs/log-.txt",
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 30,
                outputTemplate:
                "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] ({CorrelationId}) {Message:lj}{NewLine}{Exception}")
            .ReadFrom.Configuration(configuration)
            .CreateLogger();
    }
}
