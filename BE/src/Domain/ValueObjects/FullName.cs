using Backend.Domain.Common;

namespace Backend.Domain.ValueObjects;

/// <summary>
/// Value Object grouping first/last name together so they are always
/// created, validated and compared as one unit.
/// </summary>
public sealed class FullName : ValueObject
{
    public string FirstName { get; }
    public string LastName { get; }
    public string DisplayName => $"{FirstName} {LastName}".Trim();

    private FullName(string firstName, string lastName)
    {
        FirstName = firstName;
        LastName = lastName;
    }

    public static FullName Create(string? firstName, string? lastName)
    {
        if (string.IsNullOrWhiteSpace(firstName))
            throw new DomainException("First name cannot be empty.");
        if (string.IsNullOrWhiteSpace(lastName))
            throw new DomainException("Last name cannot be empty.");

        return new FullName(firstName.Trim(), lastName.Trim());
    }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return FirstName;
        yield return LastName;
    }

    public override string ToString() => DisplayName;
}
