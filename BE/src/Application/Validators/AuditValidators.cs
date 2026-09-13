using Backend.Application.DTOs.Audit;
using Backend.Shared.Constants;
using Backend.Shared.Extensions;
using FluentValidation;

namespace Backend.Application.Validators;

/// <summary>
/// T4.4 / T5 — strongly-typed validation for the System Audit Log search query.
/// EventType/Status are enums (model binding rejects invalid values with 400);
/// SortBy is whitelisted server-side; PageSize is clamped by <c>PaginationRequest</c>.
/// </summary>
public class SystemAuditLogQueryValidator : AbstractValidator<SystemAuditLogQuery>
{
    public SystemAuditLogQueryValidator()
    {
        RuleFor(x => x.ToUtc)
            .GreaterThanOrEqualTo(x => x.FromUtc!.Value)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue)
            .WithMessage("ToUtc must be greater than or equal to FromUtc.");

        RuleFor(x => x.UserName)
            .MaximumLength(ValidationConstants.UsernameMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("UserName must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.UserName));

        RuleFor(x => x.Action)
            .MaximumLength(100)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("Action must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.Action));

        RuleFor(x => x.Keyword)
            .MaximumLength(ValidationConstants.KeywordMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("Keyword must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.Keyword));
    }
}

/// <summary>T5 — bounds free-text filters on the legacy <c>app.AuditLogs</c> query API.</summary>
public class AuditLogQueryValidator : AbstractValidator<AuditLogQuery>
{
    public AuditLogQueryValidator()
    {
        RuleFor(x => x.ToUtc)
            .GreaterThanOrEqualTo(x => x.FromUtc!.Value)
            .When(x => x.FromUtc.HasValue && x.ToUtc.HasValue)
            .WithMessage("ToUtc must be greater than or equal to FromUtc.");

        RuleFor(x => x.EntityName)
            .MaximumLength(ValidationConstants.NameMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("EntityName must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.EntityName));

        RuleFor(x => x.EntityId)
            .MaximumLength(ValidationConstants.NameMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("EntityId must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.EntityId));

        RuleFor(x => x.UserId)
            .MaximumLength(ValidationConstants.AuditUserMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("UserId must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.UserId));

        RuleFor(x => x.Keyword)
            .MaximumLength(ValidationConstants.KeywordMaxLength)
            .Must(v => v is null || !v.ContainsDisallowedControlChars())
            .WithMessage("Keyword must not contain control characters.")
            .When(x => !string.IsNullOrEmpty(x.Keyword));
    }
}
