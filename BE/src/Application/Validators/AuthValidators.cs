using Backend.Application.DTOs.Auth;
using Backend.Shared.Constants;
using Backend.Shared.Extensions;
using FluentValidation;

namespace Backend.Application.Validators;

public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
{
    public RegisterRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty()
            .MaximumLength(ValidationConstants.UsernameMaxLength)
            .Must(u => u.IsSafeIdentifier())
            .WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");
        RuleFor(x => x.FullName)
            .Must((req, _) => !string.IsNullOrWhiteSpace(req.FullName) || !string.IsNullOrWhiteSpace(req.DisplayName))
            .WithMessage("FullName is required.")
            .Must((req, _) =>
            {
                var name = string.IsNullOrWhiteSpace(req.FullName) ? req.DisplayName : req.FullName;
                return name.Length <= ValidationConstants.DisplayNameMaxLength;
            })
            .WithMessage($"FullName must be at most {ValidationConstants.DisplayNameMaxLength} characters.")
            .Must((req, _) =>
            {
                var name = string.IsNullOrWhiteSpace(req.FullName) ? req.DisplayName : req.FullName;
                return string.IsNullOrEmpty(name) || !name.ContainsDisallowedControlChars();
            })
            .WithMessage("FullName must not contain control characters.");
        RuleFor(x => x.Password).NotEmpty()
            .MinimumLength(SecurityConstants.PasswordMinLength)
            .MaximumLength(SecurityConstants.PasswordMaxLength)
            .Must(HasPasswordComplexity)
            .WithMessage("Password must contain uppercase, lowercase, a digit and a special character.");
    }

    private static bool HasPasswordComplexity(string password) =>
        password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(ch => !char.IsLetterOrDigit(ch));
}

public class LoginRequestValidator : AbstractValidator<LoginRequest>
{
    public LoginRequestValidator()
    {
        // T4.4 §9 / T5 — bound username; NEVER trim/complexity the password.
        RuleFor(x => x.Username).NotEmpty()
            .MaximumLength(ValidationConstants.UsernameMaxLength)
            .Must(u => u.IsSafeIdentifier())
            .WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");
        RuleFor(x => x.Password).NotEmpty().MaximumLength(SecurityConstants.PasswordMaxLength);
    }
}

public class RefreshRequestValidator : AbstractValidator<RefreshRequest>
{
    public RefreshRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(ValidationConstants.TokenMaxLength);
    }
}

public class LogoutRequestValidator : AbstractValidator<LogoutRequest>
{
    public LogoutRequestValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(ValidationConstants.TokenMaxLength);
        RuleFor(x => x.SessionId)
            .MaximumLength(64)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("SessionId must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.SessionId));
    }
}

public class ChangePasswordRequestValidator : AbstractValidator<ChangePasswordRequest>
{
    public ChangePasswordRequestValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(SecurityConstants.PasswordMaxLength);
        RuleFor(x => x.NewPassword).NotEmpty()
            .MinimumLength(SecurityConstants.PasswordMinLength)
            .MaximumLength(SecurityConstants.PasswordMaxLength)
            .Must(HasPasswordComplexity)
            .WithMessage("Password must contain uppercase, lowercase, a digit and a special character.")
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must be different from the current password.");
    }

    private static bool HasPasswordComplexity(string password) =>
        password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(ch => !char.IsLetterOrDigit(ch));
}

public class ForgotPasswordRequestValidator : AbstractValidator<ForgotPasswordRequest>
{
    public ForgotPasswordRequestValidator()
    {
        RuleFor(x => x.Username).NotEmpty()
            .MaximumLength(ValidationConstants.UsernameMaxLength)
            .Must(u => u.IsSafeIdentifier())
            .WithMessage("Username may only contain letters, digits, '.', '_' and '-'.");
    }
}

public class ResetPasswordRequestValidator : AbstractValidator<ResetPasswordRequest>
{
    public ResetPasswordRequestValidator()
    {
        RuleFor(x => x.Token).NotEmpty().MaximumLength(ValidationConstants.TokenMaxLength);
        RuleFor(x => x.NewPassword).NotEmpty()
            .MinimumLength(SecurityConstants.PasswordMinLength)
            .MaximumLength(SecurityConstants.PasswordMaxLength)
            .Must(HasPasswordComplexity)
            .WithMessage("Password must contain uppercase, lowercase, a digit and a special character.");
    }

    private static bool HasPasswordComplexity(string password) =>
        password.Any(char.IsUpper)
        && password.Any(char.IsLower)
        && password.Any(char.IsDigit)
        && password.Any(ch => !char.IsLetterOrDigit(ch));
}

// --- Legacy IAM DTOs (app.Users) — kept while IAuthService remains registered ---

public class RegisterValidator : AbstractValidator<RegisterDto>
{
    public RegisterValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(ValidationConstants.EmailMaxLength);
        RuleFor(x => x.FirstName).NotEmpty()
            .MaximumLength(ValidationConstants.NameMaxLength)
            .Must(v => !v.ContainsDisallowedControlChars())
            .WithMessage("FirstName must not contain control characters.");
        RuleFor(x => x.LastName).NotEmpty()
            .MaximumLength(ValidationConstants.NameMaxLength)
            .Must(v => !v.ContainsDisallowedControlChars())
            .WithMessage("LastName must not contain control characters.");
        RuleFor(x => x.Password).NotEmpty()
            .MinimumLength(SecurityConstants.PasswordMinLength)
            .MaximumLength(SecurityConstants.PasswordMaxLength);
    }
}

public class LoginValidator : AbstractValidator<LoginDto>
{
    public LoginValidator()
    {
        RuleFor(x => x.Email).NotEmpty().EmailAddress().MaximumLength(ValidationConstants.EmailMaxLength);
        RuleFor(x => x.Password).NotEmpty().MaximumLength(SecurityConstants.PasswordMaxLength);
    }
}

public class RefreshTokenValidator : AbstractValidator<RefreshTokenRequestDto>
{
    public RefreshTokenValidator()
    {
        RuleFor(x => x.RefreshToken).NotEmpty().MaximumLength(ValidationConstants.TokenMaxLength);
    }
}
