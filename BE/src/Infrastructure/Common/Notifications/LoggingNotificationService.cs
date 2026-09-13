using Backend.Application.Common;
using Backend.Shared.Models;
using Microsoft.Extensions.Logging;

namespace Backend.Infrastructure.Common;

/// <summary>
/// Safe default <see cref="INotificationService"/>: it writes every notification
/// to the structured log and delivers nothing. That means feature code can call
/// the notification API from day one - and be exercised in tests, CI and local
/// runs - without an SMTP server, a push certificate or an SMS bill.
/// <para>
/// Real transports plug in by swapping the DI registration only: a SignalR hub
/// for <see cref="NotificationChannel.InApp"/>, SMTP for
/// <see cref="NotificationChannel.Email"/>, FCM/APNS for
/// <see cref="NotificationChannel.Push"/>, a gateway for
/// <see cref="NotificationChannel.Sms"/>. No Application code changes.
/// See README "Extensibility".
/// </para>
/// </summary>
public class LoggingNotificationService(ILogger<LoggingNotificationService> logger) : INotificationService
{
    public Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.Log(
            ToLogLevel(message.Severity),
            "Notification not delivered (logging default). Channel={Channel} Title={Title} Body={Body} Data={@Data}",
            message.Channel,
            message.Title,
            message.Body,
            message.Data);

        return Task.CompletedTask;
    }

    public Task SendToUserAsync(Guid userId, NotificationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.Log(
            ToLogLevel(message.Severity),
            "Notification not delivered (logging default). Recipient={UserId} Channel={Channel} Title={Title} Body={Body} Data={@Data}",
            userId,
            message.Channel,
            message.Title,
            message.Body,
            message.Data);

        return Task.CompletedTask;
    }

    public Task BroadcastAsync(NotificationMessage message, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(message);

        logger.Log(
            ToLogLevel(message.Severity),
            "Broadcast not delivered (logging default). Channel={Channel} Title={Title} Body={Body} Data={@Data}",
            message.Channel,
            message.Title,
            message.Body,
            message.Data);

        return Task.CompletedTask;
    }

    /// <summary>
    /// Mirrors the caller's severity onto the log so an undelivered Error
    /// notification is still visible to whatever is scraping the logs today.
    /// </summary>
    private static LogLevel ToLogLevel(NotificationSeverity severity) => severity switch
    {
        NotificationSeverity.Warning => LogLevel.Warning,
        NotificationSeverity.Error => LogLevel.Error,
        _ => LogLevel.Information
    };
}
