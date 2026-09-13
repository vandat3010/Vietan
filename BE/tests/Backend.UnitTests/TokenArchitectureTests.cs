using Backend.Application.Common;
using Backend.Infrastructure.Identity;
using Backend.Shared.Constants;
using Xunit;

namespace Backend.UnitTests;

public class JwtSettingsValidationTests
{
    private static JwtSettings Valid() => new()
    {
        Issuer = "Backend.Api",
        Audience = "Backend.Client",
        SigningKey = "UNIT_TEST_SIGNING_KEY_AT_LEAST_32_CHARS!!",
        AccessTokenExpirationMinutes = 15,
        RefreshTokenExpirationDays = 7
    };

    [Fact]
    public void Validate_Accepts_ProductionLikeKey() =>
        Assert.True(Valid().Validate(out _));

    [Theory]
    [InlineData("CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS_LONG")]
    [InlineData("secret")]
    [InlineData("123456")]
    [InlineData("development-secret")]
    [InlineData("short")]
    [InlineData("")]
    public void Validate_Rejects_PlaceholderOrShortKey(string key)
    {
        var settings = Valid();
        settings.SigningKey = key;
        Assert.False(settings.Validate(out var error));
        Assert.False(string.IsNullOrWhiteSpace(error));
        if (key.Length >= 8)
            Assert.DoesNotContain(key[..8], error, StringComparison.Ordinal);
    }

    [Fact]
    public void Validate_Rejects_AccessTokenLifetimeOutOfRange()
    {
        var settings = Valid();
        settings.AccessTokenExpirationMinutes = 0;
        Assert.False(settings.Validate(out _));
        settings.AccessTokenExpirationMinutes = 61;
        Assert.False(settings.Validate(out _));
    }

    [Fact]
    public void IsPlaceholderSigningKey_DetectsChangeMe() =>
        Assert.True(JwtSettings.IsPlaceholderSigningKey("CHANGE_ME_TO_A_LONG_RANDOM_SECRET_AT_LEAST_32_CHARACTERS_LONG"));
}

public class RefreshTokenSecurityPolicyTests
{
    [Fact]
    public void IsReuseOfRevokedToken_True_WhenRevoked() =>
        Assert.True(RefreshTokenSecurityPolicy.IsReuseOfRevokedToken(DateTimeOffset.UtcNow));

    [Fact]
    public void IsReuseOfRevokedToken_False_WhenActive() =>
        Assert.False(RefreshTokenSecurityPolicy.IsReuseOfRevokedToken(null));

    [Fact]
    public void LostRotationRace_OnlyWhenZeroRows()
    {
        Assert.True(RefreshTokenSecurityPolicy.LostRotationRace(0));
        Assert.False(RefreshTokenSecurityPolicy.LostRotationRace(1));
    }

    [Fact]
    public void IsExpired_RespectsNow()
    {
        var now = DateTimeOffset.UtcNow;
        Assert.True(RefreshTokenSecurityPolicy.IsExpired(now.AddSeconds(-1), now));
        Assert.False(RefreshTokenSecurityPolicy.IsExpired(now.AddDays(7), now));
    }

    [Fact]
    public void AuditAction_RefreshTokenReuse_IsStable() =>
        Assert.Equal("RefreshTokenReuse", AuditActionNames.RefreshTokenReuse);
}
