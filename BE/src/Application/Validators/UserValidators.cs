using Backend.Application.DTOs.Users;
using Backend.Shared.Constants;
using Backend.Shared.Extensions;
using FluentValidation;

namespace Backend.Application.Validators;

public class CreateUserValidator : AbstractValidator<CreateUserDto>
{
    public CreateUserValidator()
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
        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(SecurityConstants.PasswordMinLength)
            .MaximumLength(SecurityConstants.PasswordMaxLength)
            .Matches("[A-Z]").WithMessage("Password must contain at least one uppercase letter.")
            .Matches("[a-z]").WithMessage("Password must contain at least one lowercase letter.")
            .Matches("[0-9]").WithMessage("Password must contain at least one digit.");
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(ValidationConstants.PhoneNumberMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("PhoneNumber must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class UpdateUserValidator : AbstractValidator<UpdateUserDto>
{
    public UpdateUserValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty()
            .MaximumLength(ValidationConstants.NameMaxLength)
            .Must(v => !v.ContainsDisallowedControlChars())
            .WithMessage("FirstName must not contain control characters.");
        RuleFor(x => x.LastName).NotEmpty()
            .MaximumLength(ValidationConstants.NameMaxLength)
            .Must(v => !v.ContainsDisallowedControlChars())
            .WithMessage("LastName must not contain control characters.");
        RuleFor(x => x.PhoneNumber)
            .MaximumLength(ValidationConstants.PhoneNumberMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("PhoneNumber must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.PhoneNumber));
    }
}

public class ChangePasswordValidator : AbstractValidator<ChangePasswordDto>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty().MaximumLength(SecurityConstants.PasswordMaxLength);
        RuleFor(x => x.NewPassword)
            .NotEmpty()
            .MinimumLength(SecurityConstants.PasswordMinLength)
            .MaximumLength(SecurityConstants.PasswordMaxLength)
            .NotEqual(x => x.CurrentPassword).WithMessage("New password must be different from the current password.");
    }
}

public class UserSearchQueryValidator : AbstractValidator<UserSearchQuery>
{
    public UserSearchQueryValidator()
    {
        RuleFor(x => x.Keyword)
            .MaximumLength(ValidationConstants.KeywordMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("Keyword must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.Keyword));
    }
}

public class DeactivateUserQueryValidator : AbstractValidator<DeactivateUserQuery>
{
    public DeactivateUserQueryValidator()
    {
        RuleFor(x => x.Reason)
            .MaximumLength(ValidationConstants.ReasonMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars(allowNewLineAndTab: true))
            .WithMessage("Reason must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.Reason));
    }
}
