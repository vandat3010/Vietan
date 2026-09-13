using Backend.Application.Common;
using Backend.Application.Options;
using Microsoft.Extensions.Options;

namespace Backend.Infrastructure.Identity;

/// <summary>
/// Implements <see cref="IPasswordExpirationPolicy"/> from <see cref="PasswordPolicyOptions"/>.
/// Pure math — used identically by login, /me and the expiration worker.
/// </summary>
public sealed class PasswordExpirationPolicy(IOptions<PasswordPolicyOptions> options) : IPasswordExpirationPolicy
{
    private readonly PasswordPolicyOptions _options = options.Value;

    public PasswordExpirationStatus Evaluate(DateTimeOffset? passwordUpdatedAt, DateTimeOffset nowUtc)
    {
        // Legacy/grandfathered user: no basis to expire until they next set a password.
        if (passwordUpdatedAt is not { } updatedAt)
            return new PasswordExpirationStatus();

        var expiresAt = updatedAt.AddDays(_options.ExpireDays);
        var remaining = expiresAt - nowUtc;

        if (remaining <= TimeSpan.Zero)
            return new PasswordExpirationStatus { ExpiresAt = expiresAt, ExpiresInDays = 0, IsExpired = true };

        // Ceiling on whole days so "13 days 5 hours" shows as 14, never an early 13.
        var days = (int)Math.Ceiling(remaining.TotalHours / 24.0);
        var shouldWarn = days <= _options.WarnBeforeDays;

        return new PasswordExpirationStatus
        {
            ExpiresAt = expiresAt,
            ExpiresInDays = days,
            IsExpired = false,
            ShouldWarn = shouldWarn
        };
    }

    public DateTimeOffset ExpirationCutoffUtc(DateTimeOffset nowUtc) => nowUtc.AddDays(-_options.ExpireDays);
}
