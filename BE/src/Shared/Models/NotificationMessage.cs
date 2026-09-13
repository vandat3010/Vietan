namespace Backend.Shared.Models;

/// <summary>
/// Transport-agnostic payload handed to <c>INotificationService</c>. Lives in
/// Shared (not Application) so an Api endpoint or a Domain-adjacent caller can
/// build one without dragging in the Application layer, mirroring
/// <see cref="FileUploadResult"/>.
/// <para>
/// The shape is deliberately the lowest common denominator of in-app / e-mail /
/// push / SMS payloads: a subject line, a body and a free-form
/// <see cref="Data"/> bag. Anything provider-specific (HTML templates, FCM
/// topics, SMS sender ids) belongs in the implementation, not here - otherwise
/// callers end up coupled to whichever provider is wired up today.
/// </para>
/// </summary>
public class NotificationMessage
{
    public required string Title { get; init; }
    public required string Body { get; init; }

    /// <summary>
    /// Defaults to <see cref="NotificationChannel.InApp"/> because it is the only
    /// channel that cannot cost money or reach a real inbox by accident; opting
    /// into Email/Push/Sms must be a conscious decision at the call site.
    /// </summary>
    public NotificationChannel Channel { get; init; } = NotificationChannel.InApp;

    public NotificationSeverity Severity { get; init; } = NotificationSeverity.Information;

    /// <summary>
    /// Provider-specific extras (deep-link route, entity id, template tokens).
    /// Kept as string/string so it survives serialization to any transport.
    /// </summary>
    public IDictionary<string, string>? Data { get; init; }
}

/// <summary>Delivery transport a <see cref="NotificationMessage"/> is destined for.</summary>
public enum NotificationChannel
{
    InApp,
    Email,
    Push,
    Sms
}

/// <summary>
/// Importance hint. Implementations map it to whatever their transport
/// understands (log level, e-mail priority header, push notification colour).
/// </summary>
public enum NotificationSeverity
{
    Information,
    Warning,
    Error
}
