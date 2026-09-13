using Backend.Application.Common;
using Backend.Domain.Enums;
using Backend.Shared.Constants;
using Xunit;

namespace Backend.UnitTests;

/// <summary>
/// BE 3.1a — pure/unit coverage for the System Audit Log building blocks:
/// which errors get audited, and that secrets never reach the audit table.
/// </summary>
public class SystemAuditLogTests
{
    // --- AuditErrorPolicy: only security-relevant + system failures are audited (§4) ---

    [Theory]
    [InlineData(401, true)]
    [InlineData(403, true)]
    [InlineData(500, true)]
    [InlineData(502, true)]
    [InlineData(503, true)]
    public void ShouldAudit_SecurityAndServerErrors_True(int statusCode, bool expected) =>
        Assert.Equal(expected, AuditErrorPolicy.ShouldAudit(statusCode));

    [Theory]
    [InlineData(200)]
    [InlineData(201)]
    [InlineData(400)] // validation - handled business response, not audited
    [InlineData(404)]
    [InlineData(409)]
    [InlineData(429)]
    public void ShouldAudit_NormalAndValidationErrors_False(int statusCode) =>
        Assert.False(AuditErrorPolicy.ShouldAudit(statusCode));

    [Theory]
    [InlineData(401, AuditEventType.ApiError)]
    [InlineData(403, AuditEventType.ApiError)]
    [InlineData(500, AuditEventType.SystemError)]
    [InlineData(503, AuditEventType.SystemError)]
    public void EventTypeFor_MapsCorrectly(int statusCode, AuditEventType expected) =>
        Assert.Equal(expected, AuditErrorPolicy.EventTypeFor(statusCode));

    [Theory]
    [InlineData(401, "ApiError")]
    [InlineData(500, "SystemError")]
    public void ActionFor_MapsCorrectly(int statusCode, string expected) =>
        Assert.Equal(expected, AuditErrorPolicy.ActionFor(statusCode));

    // --- AuditSanitizer: secrets must be redacted, everything else preserved (§3/§26) ---

    [Theory]
    [InlineData("password")]
    [InlineData("Password")]
    [InlineData("newPassword")]
    [InlineData("passwordHash")]
    [InlineData("token")]
    [InlineData("accessToken")]
    [InlineData("refreshToken")]
    [InlineData("jwt")]
    [InlineData("Authorization")]
    [InlineData("apiKey")]
    [InlineData("api_key")]
    [InlineData("clientSecret")]
    [InlineData("ConnectionString")]
    [InlineData("Cookie")]
    public void IsSensitiveKey_DetectsSecrets(string key) =>
        Assert.True(AuditSanitizer.IsSensitiveKey(key));

    [Theory]
    [InlineData("username")]
    [InlineData("keyword")]
    [InlineData("format")]
    [InlineData("recordCount")]
    [InlineData("ipAddress")]
    [InlineData(null)]
    [InlineData("")]
    public void IsSensitiveKey_AllowsSafeKeys(string? key) =>
        Assert.False(AuditSanitizer.IsSensitiveKey(key));

    [Fact]
    public void Sanitize_RedactsSensitiveValues_KeepsSafeOnes()
    {
        var input = new Dictionary<string, object?>
        {
            ["username"] = "admin",
            ["password"] = "SuperSecret123!",
            ["refreshToken"] = "eyJhbGciOi...",
            ["Authorization"] = "Bearer abc.def.ghi",
            ["format"] = "xlsx",
            ["sizeBytes"] = 2048
        };

        var result = AuditSanitizer.Sanitize(input);

        Assert.Equal("admin", result["username"]);
        Assert.Equal("xlsx", result["format"]);
        Assert.Equal(2048, result["sizeBytes"]);
        Assert.Equal(AuditSanitizer.Redacted, result["password"]);
        Assert.Equal(AuditSanitizer.Redacted, result["refreshToken"]);
        Assert.Equal(AuditSanitizer.Redacted, result["Authorization"]);

        // Absolutely no raw secret value survives anywhere in the sanitized payload.
        var serialized = string.Join("|", result.Values);
        Assert.DoesNotContain("SuperSecret123!", serialized);
        Assert.DoesNotContain("eyJhbGciOi", serialized);
        Assert.DoesNotContain("Bearer abc.def.ghi", serialized);
    }

    [Fact]
    public void Sanitize_NullInput_ReturnsEmpty() =>
        Assert.Empty(AuditSanitizer.Sanitize(null));

    [Fact]
    public void Truncate_LongText_IsBounded()
    {
        var longText = new string('x', AuditSanitizer.MaxTextLength + 500);
        var result = AuditSanitizer.Truncate(longText);
        Assert.Equal(AuditSanitizer.MaxTextLength, result!.Length);
    }

    [Fact]
    public void Truncate_ShortText_Unchanged() =>
        Assert.Equal("hello", AuditSanitizer.Truncate("hello"));

    [Fact]
    public void Truncate_Null_ReturnsNull() =>
        Assert.Null(AuditSanitizer.Truncate(null));

    // --- Constants / enums remain stable (contract used by the FE + queries) ---

    [Fact]
    public void AuditActionNames_AreStable()
    {
        Assert.Equal("Login", AuditActionNames.Login);
        Assert.Equal("Logout", AuditActionNames.Logout);
        Assert.Equal("Export", AuditActionNames.Export);
        Assert.Equal("ApiError", AuditActionNames.ApiError);
        Assert.Equal("SystemError", AuditActionNames.SystemError);
    }

    [Fact]
    public void AuditEnums_HaveExpectedMembers()
    {
        Assert.Equal("Authentication", AuditEventType.Authentication.ToString());
        Assert.Equal("DataExport", AuditEventType.DataExport.ToString());
        Assert.Equal("ApiError", AuditEventType.ApiError.ToString());
        Assert.Equal("SystemError", AuditEventType.SystemError.ToString());
        Assert.Equal("Success", AuditStatus.Success.ToString());
        Assert.Equal("Failed", AuditStatus.Failed.ToString());
    }
}
