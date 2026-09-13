using Backend.Application.DTOs.Audit;
using Backend.Application.DTOs.Auth;
using Backend.Application.Validators;
using Backend.Shared.Constants;
using Xunit;

namespace Backend.UnitTests;

/// <summary>
/// T4 — coverage for the new security-audit action names and the input validation
/// added for login / password-change / audit search.
/// </summary>
public class T4SecurityAuditTests
{
    // --- T4.2: action-name constants (string-based → no persisted-enum reorder risk) ---

    [Fact]
    public void AuditActionNames_NewSecurityActions_AreStable()
    {
        Assert.Equal("LoginFailed", AuditActionNames.LoginFailed);
        Assert.Equal("AccountLocked", AuditActionNames.AccountLocked);
        Assert.Equal("PasswordChanged", AuditActionNames.PasswordChanged);
        Assert.Equal("ConfigurationChanged", AuditActionNames.ConfigurationChanged);
    }

    // --- T4.4 §9: login username length; password never gets a complexity/trim rule ---

    [Fact]
    public void LoginValidator_EmptyUsername_Fails()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest { Username = "", Password = "x" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginValidator_OverlongUsername_Fails()
    {
        var result = new LoginRequestValidator().Validate(
            new LoginRequest { Username = new string('a', 101), Password = "x" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void LoginValidator_SimplePassword_IsAccepted()
    {
        // Login must NOT enforce complexity — that would break existing accounts.
        var result = new LoginRequestValidator().Validate(new LoginRequest { Username = "admin", Password = "abc" });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void LoginValidator_OversizedPassword_Fails()
    {
        var result = new LoginRequestValidator().Validate(
            new LoginRequest { Username = "admin", Password = new string('x', SecurityConstants.PasswordMaxLength + 1) });
        Assert.False(result.IsValid);
    }

    // --- T4.4 §10: change-password rules ---

    [Fact]
    public void ChangePasswordValidator_NewEqualsCurrent_Fails()
    {
        var result = new ChangePasswordRequestValidator().Validate(
            new ChangePasswordRequest { CurrentPassword = "Abcd1234!", NewPassword = "Abcd1234!" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangePasswordValidator_WeakNewPassword_Fails()
    {
        var result = new ChangePasswordRequestValidator().Validate(
            new ChangePasswordRequest { CurrentPassword = "Abcd1234!", NewPassword = "alllowercase" });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void ChangePasswordValidator_StrongDifferentPassword_Passes()
    {
        var result = new ChangePasswordRequestValidator().Validate(
            new ChangePasswordRequest { CurrentPassword = "Abcd1234!", NewPassword = "Xyz9876#$" });
        Assert.True(result.IsValid);
    }

    // --- T4.4 §14: audit search date-range validation ---

    [Fact]
    public void AuditQueryValidator_FromAfterTo_Fails()
    {
        var result = new SystemAuditLogQueryValidator().Validate(new SystemAuditLogQuery
        {
            FromUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero),
            ToUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero)
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void AuditQueryValidator_ValidRange_Passes()
    {
        var result = new SystemAuditLogQueryValidator().Validate(new SystemAuditLogQuery
        {
            FromUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
            ToUtc = new DateTimeOffset(2026, 6, 1, 0, 0, 0, TimeSpan.Zero)
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void AuditQueryValidator_OnlyOneBound_Passes()
    {
        var validator = new SystemAuditLogQueryValidator();
        Assert.True(validator.Validate(new SystemAuditLogQuery { FromUtc = DateTimeOffset.UtcNow }).IsValid);
        Assert.True(validator.Validate(new SystemAuditLogQuery { ToUtc = DateTimeOffset.UtcNow }).IsValid);
        Assert.True(validator.Validate(new SystemAuditLogQuery()).IsValid);
    }

    [Fact]
    public void AuditQueryValidator_OverlongUserName_Fails()
    {
        var result = new SystemAuditLogQueryValidator().Validate(
            new SystemAuditLogQuery { UserName = new string('u', 101) });
        Assert.False(result.IsValid);
    }
}
