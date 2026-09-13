using Backend.Shared.Models;

namespace Backend.Application.Common;

/// <summary>
/// Outbound notification dispatch. The Application layer decides *what* to say
/// and *who* to say it to; the implementation decides *how* it travels
/// (SignalR hub, SMTP, FCM, an SMS gateway), so adding a transport is a DI
/// registration change only. See README "Extensibility".
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Sends a message whose audience is already encoded in the message itself
    /// (or in the transport's own configuration, e.g. an ops alert channel).
    /// </summary>
    Task SendAsync(NotificationMessage message, CancellationToken cancellationToken = default);

    Task SendToUserAsync(Guid userId, NotificationMessage message, CancellationToken cancellationToken = default);

    /// <summary>Fans the message out to every connected/subscribed recipient.</summary>
    Task BroadcastAsync(NotificationMessage message, CancellationToken cancellationToken = default);
}
