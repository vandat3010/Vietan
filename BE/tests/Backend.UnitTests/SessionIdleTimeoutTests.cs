using Backend.Application.Common;
using Backend.Domain.Entities.Scada;

namespace Backend.UnitTests;

/// <summary>BE 2.1 T3.1 — idle-timeout config parsing/validation (invalid never becomes 0/infinite silently).</summary>
public class SessionIdleTimeoutPolicyTests
{
    [Theory]
    [InlineData("10", 10)]
    [InlineData("1", 1)]
    [InlineData("  15  ", 15)] // trimmed
    [InlineData("120", 120)]
    public void TryParseMinutes_Accepts_Positive_Integers(string raw, int expected)
    {
        Assert.True(SessionIdleTimeoutPolicy.TryParseMinutes(raw, out var minutes));
        Assert.Equal(expected, minutes);
    }

    [Theory]
    [InlineData("0")]      // Test 3: zero → invalid
    [InlineData("-5")]     // negative → invalid
    [InlineData(null)]     // missing → invalid
    [InlineData("")]       // empty → invalid
    [InlineData("   ")]    // whitespace → invalid
    [InlineData("abc")]    // non-numeric → invalid
    [InlineData("10.5")]   // non-integer → invalid
    [InlineData("9999999999999")] // overflow → invalid
    public void TryParseMinutes_Rejects_Invalid_And_Falls_Back_To_Default(string? raw)
    {
        Assert.False(SessionIdleTimeoutPolicy.TryParseMinutes(raw, out var minutes));
        Assert.Equal(SessionIdleTimeoutPolicy.DefaultMinutes, minutes);
    }

    [Fact]
    public void ResolveMinutes_Returns_Value_Or_Default()
    {
        Assert.Equal(30, SessionIdleTimeoutPolicy.ResolveMinutes("30"));
        Assert.Equal(SessionIdleTimeoutPolicy.DefaultMinutes, SessionIdleTimeoutPolicy.ResolveMinutes("bad"));
    }

    [Fact]
    public void SettingKey_Is_The_Documented_Contract_Key() =>
        Assert.Equal("session.idleTimeoutMinutes", SessionIdleTimeoutPolicy.SettingKey);
}

/// <summary>
/// BE 2.1 T3.3 — domain invariant behind "old refresh token cannot refresh after
/// logout". Logout sets <c>RevokedAt</c>; RefreshAsync gates on <c>IsActive</c>.
/// </summary>
public class ScadaRefreshTokenStateTests
{
    private static readonly DateTimeOffset Now = DateTimeOffset.UtcNow;

    [Fact]
    public void IsActive_True_When_Neither_Revoked_Nor_Expired()
    {
        var token = new ScadaRefreshToken { ExpiresAt = Now.AddDays(7) };
        Assert.True(token.IsActive);
    }

    [Fact]
    public void IsActive_False_After_Logout_Revocation() // Test 5: refresh after logout rejected
    {
        var token = new ScadaRefreshToken { ExpiresAt = Now.AddDays(7), RevokedAt = Now };
        Assert.False(token.IsActive);
    }

    [Fact]
    public void IsActive_False_When_Expired()
    {
        var token = new ScadaRefreshToken { ExpiresAt = Now.AddSeconds(-1) };
        Assert.False(token.IsActive);
    }
}
