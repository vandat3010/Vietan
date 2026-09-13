using System.Text.RegularExpressions;
using Backend.Domain.Common;

namespace Backend.Domain.ValueObjects;

/// <summary>
/// Value Object wrapping an e-mail address. Guarantees that a valid, normalized
/// (lower-cased) e-mail is the only way an <see cref="Email"/> instance can exist,
/// so no other layer needs to re-validate it.
/// </summary>
public sealed partial class Email : ValueObject
{
    public string Value { get; }

    private Email(string value) => Value = value;

    public static Email Create(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            throw new DomainException("Email cannot be empty.");

        var normalized = value.Trim().ToLowerInvariant();

        if (!EmailRegex().IsMatch(normalized))
            throw new DomainException($"'{value}' is not a valid email address.");

        return new Email(normalized);
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Value;
    }

    public override string ToString() => Value;

    public static implicit operator string(Email email) => email.Value;

    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex EmailRegex();
}
