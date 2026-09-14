using Backend.Application.DTOs.Auth;
using Backend.Application.DTOs.Roles;
using Backend.Application.DTOs.Users;
using Backend.Application.Options;
using Backend.Application.Validators;
using Backend.Shared.Constants;
using Backend.Shared.Extensions;
using Microsoft.Extensions.Options;
using Xunit;

namespace Backend.UnitTests;

/// <summary>T5 — free-text length/control-char validation and HTML sanitizer helper.</summary>
public class T5FreeTextValidationTests
{
    private static RegisterRequestValidator CreateRegisterValidator() =>
        new(Options.Create(new PasswordPolicyOptions()));

    // --- Identifier / control characters ---

    [Theory]
    [InlineData("admin")]
    [InlineData("user9")]
    [InlineData("user_1")]
    [InlineData("op-01")]
    [InlineData("a.b")]
    public void IsSafeIdentifier_AcceptsWhitelist(string value) =>
        Assert.True(value.IsSafeIdentifier());

    [Theory]
    [InlineData("")]
    [InlineData("admin admin")]
    [InlineData("user\n1")]
    [InlineData("user<script>")]
    [InlineData("Bơm1")]
    public void IsSafeIdentifier_RejectsOther(string value) =>
        Assert.False(value.IsSafeIdentifier());

    [Fact]
    public void IsSafeIdentifier_Null_IsFalse() =>
        Assert.False(((string?)null).IsSafeIdentifier());

    [Fact]
    public void DisplayName_Vietnamese_HasNoDisallowedControls() =>
        Assert.False("Thiết bị áp suất cao".ContainsDisallowedControlChars());

    [Fact]
    public void Description_AllowsNewLine_WhenFlagSet()
    {
        const string text = "Bơm số 1 - Khu vực A\nPressure = 10.5 bar";
        Assert.False(text.ContainsDisallowedControlChars(allowNewLineAndTab: true));
        Assert.True(text.ContainsDisallowedControlChars(allowNewLineAndTab: false));
    }

    [Fact]
    public void ControlChar_OtherThanNewLine_AlwaysRejected()
    {
        var withNull = "hello\0world";
        Assert.True(withNull.ContainsDisallowedControlChars(allowNewLineAndTab: true));
        Assert.True(withNull.ContainsDisallowedControlChars(allowNewLineAndTab: false));
    }

    // --- SanitizeHtml: only for actual HTML fields; null/empty unchanged ---

    [Fact]
    public void SanitizeHtml_Null_ReturnsNull() =>
        Assert.Null(((string?)null).SanitizeHtml());

    [Fact]
    public void SanitizeHtml_Empty_ReturnsEmpty() =>
        Assert.Equal(string.Empty, string.Empty.SanitizeHtml());

    [Fact]
    public void SanitizeHtml_PlainVietnamese_Unchanged()
    {
        const string text = "Bơm số 1 - Khu vực A";
        Assert.Equal(text, text.SanitizeHtml());
    }

    [Fact]
    public void SanitizeHtml_AllowsSafeTags()
    {
        const string html = "<p>Motor <strong>#2</strong></p>";
        var result = html.SanitizeHtml();
        Assert.Contains("<p>", result);
        Assert.Contains("<strong>", result);
        Assert.Contains("Motor", result);
    }

    [Fact]
    public void SanitizeHtml_StripsScript()
    {
        var result = "<script>alert('XSS')</script>ok".SanitizeHtml();
        Assert.DoesNotContain("<script", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result);
        Assert.Contains("ok", result);
    }

    [Fact]
    public void SanitizeHtml_StripsEventHandlerImage()
    {
        var result = "<img src=x onerror=alert(1)>safe".SanitizeHtml();
        Assert.DoesNotContain("<img", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("onerror", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result);
        Assert.Contains("safe", result);
    }

    [Fact]
    public void SanitizeHtml_StripsJavascriptUrl()
    {
        var result = "<a href=\"javascript:alert(1)\">click</a>".SanitizeHtml() ?? string.Empty;
        Assert.DoesNotContain("javascript:", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("<a", result, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("alert", result, StringComparison.OrdinalIgnoreCase);
    }

    // --- FluentValidation: Register DisplayName / Username ---

    [Fact]
    public void Register_VietnameseDisplayName_Passes()
    {
        var result = CreateRegisterValidator().Validate(new RegisterRequest
        {
            Username = "operator1",
            DisplayName = "Thiết bị áp suất cao",
            Password = "Abcd1234!"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_DisplayNameExactlyMax_Passes()
    {
        var result = CreateRegisterValidator().Validate(new RegisterRequest
        {
            Username = "operator1",
            DisplayName = new string('A', ValidationConstants.DisplayNameMaxLength),
            Password = "Abcd1234!"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Register_DisplayNameOverMax_Fails()
    {
        var result = CreateRegisterValidator().Validate(new RegisterRequest
        {
            Username = "operator1",
            DisplayName = new string('A', ValidationConstants.DisplayNameMaxLength + 1),
            Password = "Abcd1234!"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Register_WhitespaceDisplayName_Fails()
    {
        var result = CreateRegisterValidator().Validate(new RegisterRequest
        {
            Username = "operator1",
            DisplayName = "   ",
            Password = "Abcd1234!"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Register_UsernameWithNewline_Fails()
    {
        var result = CreateRegisterValidator().Validate(new RegisterRequest
        {
            Username = "admin\nroot",
            DisplayName = "Admin",
            Password = "Abcd1234!"
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Register_DisplayNameWithControlChar_Fails()
    {
        var result = CreateRegisterValidator().Validate(new RegisterRequest
        {
            Username = "operator1",
            DisplayName = "Admin\0",
            Password = "Abcd1234!"
        });
        Assert.False(result.IsValid);
    }

    // --- Role Description (multiline free-text) ---

    [Fact]
    public void CreateRole_MultilineVietnameseDescription_Passes()
    {
        var result = new CreateRoleValidator().Validate(new CreateRoleDto
        {
            Name = "Operator",
            Description = "Bơm số 1 - Khu vực A\nPressure = 10.5 bar\nMotor #2"
        });
        Assert.True(result.IsValid);
    }

    [Fact]
    public void CreateRole_DescriptionOverMax_Fails()
    {
        var result = new CreateRoleValidator().Validate(new CreateRoleDto
        {
            Name = "Operator",
            Description = new string('x', ValidationConstants.DescriptionMaxLength + 1)
        });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateRole_NameWithNewline_Fails()
    {
        var result = new CreateRoleValidator().Validate(new CreateRoleDto { Name = "Ad\nmin" });
        Assert.False(result.IsValid);
    }

    // --- Deactivate reason ---

    [Fact]
    public void Deactivate_ReasonWithScript_IsPlainTextAccepted_ButNoControlExceptNewLine()
    {
        // Stored as plain text; XSS is an output-encoding concern. Length + controls only.
        var ok = new DeactivateUserQueryValidator().Validate(
            new DeactivateUserQuery { Reason = "<script>alert('XSS')</script>" });
        Assert.True(ok.IsValid);

        var bad = new DeactivateUserQueryValidator().Validate(
            new DeactivateUserQuery { Reason = "x\0y" });
        Assert.False(bad.IsValid);
    }

    [Fact]
    public void Deactivate_ReasonOverMax_Fails()
    {
        var result = new DeactivateUserQueryValidator().Validate(
            new DeactivateUserQuery { Reason = new string('r', ValidationConstants.ReasonMaxLength + 1) });
        Assert.False(result.IsValid);
    }

    [Fact]
    public void CreateUser_VietnameseNames_Pass()
    {
        var result = new CreateUserValidator().Validate(new CreateUserDto
        {
            Email = "a@b.co",
            FirstName = "Nguyễn",
            LastName = "Văn A",
            Password = "Abcdefg1"
        });
        Assert.True(result.IsValid);
    }
}
