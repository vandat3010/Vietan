namespace Backend.Shared.Extensions;

/// <summary>Reusable <see cref="DateTime"/> helpers used across every layer.</summary>
public static class DateTimeExtensions
{
    private static readonly DateTime UnixEpoch = new(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    /// <summary>Seconds since the Unix epoch (UTC) - handy for JWT <c>exp</c>/<c>iat</c> claims.</summary>
    public static long ToUnixTimestamp(this DateTime value) =>
        (long)(value.ToUniversalTime() - UnixEpoch).TotalSeconds;

    /// <summary>Midnight (00:00:00.000) of the same date, same <see cref="DateTimeKind"/>.</summary>
    public static DateTime StartOfDay(this DateTime value) =>
        new(value.Year, value.Month, value.Day, 0, 0, 0, value.Kind);

    /// <summary>The last instant (23:59:59.9999999) of the same date, same <see cref="DateTimeKind"/>.</summary>
    public static DateTime EndOfDay(this DateTime value) =>
        value.StartOfDay().AddDays(1).AddTicks(-1);
}
