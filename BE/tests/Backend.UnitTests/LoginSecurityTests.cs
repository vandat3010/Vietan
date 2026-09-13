using Backend.Application.Common;
using Backend.Application.Interfaces.Services;
using Backend.Application.Options;
using Backend.Infrastructure.Identity;

namespace Backend.UnitTests;

/// <summary>BE 1.4 — pure lockout arithmetic (shared, side-effect-free).</summary>
public class LoginLockoutPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 10, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData(1, 5, 4)] // Test 1: first failure → remaining = max - 1
    [InlineData(2, 5, 3)]
    [InlineData(3, 5, 2)]
    [InlineData(4, 5, 1)]
    [InlineData(5, 5, 0)] // never negative even at the threshold
    [InlineData(7, 5, 0)] // never negative beyond the threshold
    public void RemainingAttempts_Is_NonNegative_Countdown(int failures, int max, int expected) =>
        Assert.Equal(expected, LoginLockoutPolicy.RemainingAttempts(failures, max));

    [Theory]
    [InlineData(4, 5, false)]
    [InlineData(5, 5, true)] // Test 3: threshold reached
    [InlineData(6, 5, true)]
    [InlineData(1, 0, false)] // guards against a mis-configured max = 0
    public void ThresholdReached_At_Or_Above_Max(int failures, int max, bool expected) =>
        Assert.Equal(expected, LoginLockoutPolicy.ThresholdReached(failures, max));

    [Fact]
    public void ComputeLockoutUntil_From_No_Existing_Lock_Adds_Window()
    {
        var until = LoginLockoutPolicy.ComputeLockoutUntil(null, Now, 15);
        Assert.Equal(Now.AddMinutes(15), until);
    }

    [Fact]
    public void ComputeLockoutUntil_Never_Shortens_An_Existing_Lock()
    {
        // Existing lock is further in the future than a fresh 15-minute window.
        var existing = Now.AddMinutes(30);
        var until = LoginLockoutPolicy.ComputeLockoutUntil(existing, Now, 15);
        Assert.Equal(existing, until); // monotonic: keeps the longer lock
    }

    [Fact]
    public void ComputeLockoutUntil_Extends_When_New_Window_Is_Longer()
    {
        var existing = Now.AddMinutes(5);
        var until = LoginLockoutPolicy.ComputeLockoutUntil(existing, Now, 15);
        Assert.Equal(Now.AddMinutes(15), until);
    }

    [Fact]
    public void IsLockedOut_True_While_Lock_In_Future() =>
        Assert.True(LoginLockoutPolicy.IsLockedOut(Now.AddMinutes(1), Now)); // Test 4

    [Fact]
    public void IsLockedOut_False_When_Expired() =>
        Assert.False(LoginLockoutPolicy.IsLockedOut(Now.AddMinutes(-1), Now)); // Test 5

    [Fact]
    public void IsLockedOut_False_When_Null() =>
        Assert.False(LoginLockoutPolicy.IsLockedOut(null, Now));

    [Fact]
    public void LockoutRetryAfterSeconds_Rounds_Up_And_Never_Negative()
    {
        Assert.Equal(90, LoginLockoutPolicy.LockoutRetryAfterSeconds(Now.AddSeconds(90), Now));
        Assert.Equal(0, LoginLockoutPolicy.LockoutRetryAfterSeconds(Now.AddSeconds(-5), Now));
        Assert.Equal(0, LoginLockoutPolicy.LockoutRetryAfterSeconds(null, Now));
    }
}

/// <summary>BE 1.4 — Redis key construction (username + IP), no Redis required.</summary>
public class RedisLoginAttemptKeyTests
{
    private const string Prefix = "tln:auth:login-failed";

    [Fact]
    public void BuildKey_Uses_Prefix_Username_And_Ip()
    {
        var key = RedisLoginAttemptService.BuildKey(Prefix, "admin", "192.168.1.10");
        Assert.Equal("tln:auth:login-failed:admin:192.168.1.10", key);
    }

    [Fact]
    public void BuildKey_Trims_Username_To_Match_Auth_Lookup() // Test 11 (auth trims, case-sensitive)
    {
        var key = RedisLoginAttemptService.BuildKey(Prefix, "  admin  ", "10.0.0.1");
        Assert.Equal("tln:auth:login-failed:admin:10.0.0.1", key);
    }

    [Fact]
    public void BuildKey_Same_Username_Different_Ip_Are_Independent() // Test 9
    {
        var a = RedisLoginAttemptService.BuildKey(Prefix, "admin", "10.0.0.1");
        var b = RedisLoginAttemptService.BuildKey(Prefix, "admin", "10.0.0.2");
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void BuildKey_Same_Username_Same_Ip_Share_Counter() // Test 10
    {
        var a = RedisLoginAttemptService.BuildKey(Prefix, "admin", "10.0.0.1");
        var b = RedisLoginAttemptService.BuildKey(Prefix, "admin", "10.0.0.1");
        Assert.Equal(a, b);
    }

    [Fact]
    public void BuildKey_Canonicalizes_IPv4_Mapped_IPv6()
    {
        var mapped = RedisLoginAttemptService.BuildKey(Prefix, "admin", "::ffff:192.168.1.10");
        var plain = RedisLoginAttemptService.BuildKey(Prefix, "admin", "192.168.1.10");
        Assert.Equal(plain, mapped);
    }

    [Fact]
    public void BuildKey_Uses_Placeholder_For_Missing_Ip()
    {
        var key = RedisLoginAttemptService.BuildKey(Prefix, "admin", null);
        Assert.Equal("tln:auth:login-failed:admin:unknown", key);
    }
}

/// <summary>BE 1.4 — configuration validation guards against silently disabling protection.</summary>
public class LoginSecurityOptionsTests
{
    private static LoginSecurityOptions Valid() => new()
    {
        MaxFailed = 5,
        WindowMinutes = 5,
        LockMinutes = 15,
        RedisKeyPrefix = "tln:auth:login-failed"
    };

    [Fact]
    public void Validate_Accepts_A_Sane_Config() =>
        Assert.True(Valid().Validate(out _));

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Rejects_NonPositive_MaxFailed(int max)
    {
        var o = Valid();
        o.MaxFailed = max;
        Assert.False(o.Validate(out _));
    }

    [Fact]
    public void Validate_Rejects_NonPositive_WindowMinutes()
    {
        var o = Valid();
        o.WindowMinutes = 0;
        Assert.False(o.Validate(out _));
    }

    [Fact]
    public void Validate_Rejects_NonPositive_LockMinutes()
    {
        var o = Valid();
        o.LockMinutes = 0;
        Assert.False(o.Validate(out _));
    }

    [Fact]
    public void Validate_Rejects_Empty_Redis_Prefix()
    {
        var o = Valid();
        o.RedisKeyPrefix = "  ";
        Assert.False(o.Validate(out _));
    }

    [Fact]
    public void Validate_Rejects_Invalid_RateLimit_When_Enabled()
    {
        var o = Valid();
        o.RateLimitEnabled = true;
        o.RateLimitPermitPerWindow = 0;
        Assert.False(o.Validate(out _));
    }
}

/// <summary>BE 1.4 §16 — the fail-closed outcome must never masquerade as "0 failures".</summary>
public class LoginAttemptOutcomeTests
{
    [Fact]
    public void Unavailable_Signals_Counter_Not_Available()
    {
        var outcome = LoginAttemptOutcome.Unavailable;
        Assert.False(outcome.CounterAvailable);
    }

    [Fact]
    public void Counted_Carries_The_Failure_Count()
    {
        var outcome = LoginAttemptOutcome.Counted(3);
        Assert.True(outcome.CounterAvailable);
        Assert.Equal(3, outcome.FailureCount);
    }
}
