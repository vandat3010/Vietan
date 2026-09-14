using Backend.Application.DTOs.Scada;
using Backend.Shared.Constants;
using FluentValidation;

namespace Backend.Application.Validators;

public class UpdateScadaUserRequestValidator : AbstractValidator<UpdateScadaUserRequest>
{
    public UpdateScadaUserRequestValidator()
    {
        RuleFor(x => x.FullName)
            .MaximumLength(ValidationConstants.DisplayNameMaxLength)
            .When(x => x.FullName is not null);

        RuleFor(x => x.Email)
            .EmailAddress()
            .MaximumLength(ValidationConstants.EmailMaxLength)
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.Department).MaximumLength(200).When(x => x.Department is not null);
        RuleFor(x => x.Position).MaximumLength(200).When(x => x.Position is not null);
        RuleFor(x => x.Unit).MaximumLength(200).When(x => x.Unit is not null);
        RuleFor(x => x.Description).MaximumLength(ValidationConstants.DescriptionMaxLength).When(x => x.Description is not null);

        RuleFor(x => x.Level)
            .InclusiveBetween(1, 100)
            .When(x => x.Level is not null);

        RuleFor(x => x.Role)
            .Must(r => r is null
                || r.Equals("viewer", StringComparison.OrdinalIgnoreCase)
                || r.Equals("operator", StringComparison.OrdinalIgnoreCase)
                || r.Equals("admin", StringComparison.OrdinalIgnoreCase)
                || r.Equals("administrator", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Role must be viewer, Operator, or Administrator.")
            .When(x => x.Role is not null);
    }
}

public class UpdateStationRequestValidator : AbstractValidator<UpdateStationRequest>
{
    public UpdateStationRequestValidator()
    {
        RuleFor(x => x.Name)
            .MaximumLength(200)
            .When(x => x.Name is not null);

        RuleFor(x => x.Address).MaximumLength(500).When(x => x.Address is not null);
        RuleFor(x => x.Description).MaximumLength(ValidationConstants.DescriptionMaxLength).When(x => x.Description is not null);

        RuleFor(x => x.Latitude)
            .InclusiveBetween(-90, 90)
            .When(x => x.Latitude is not null);

        RuleFor(x => x.Longitude)
            .InclusiveBetween(-180, 180)
            .When(x => x.Longitude is not null);
    }
}

public class UpdateSessionPolicyRequestValidator : AbstractValidator<UpdateSessionPolicyRequest>
{
    public UpdateSessionPolicyRequestValidator()
    {
        RuleFor(x => x.IdleTimeoutMinutes)
            .InclusiveBetween(1, 10080)
            .WithMessage("IdleTimeoutMinutes must be between 1 and 10080 (7 days).");
    }
}
