using System.ComponentModel;
using System.Reflection;

namespace Backend.Shared.Extensions;

/// <summary>Reusable <see cref="Enum"/> helpers used across every layer.</summary>
public static class EnumExtensions
{
    /// <summary>
    /// Returns the value of a <see cref="DescriptionAttribute"/> decorating the enum
    /// member (falls back to <c>ToString()</c> when none is present) - useful for
    /// human-readable labels in API responses/exports without a separate lookup table.
    /// </summary>
    public static string GetDescription(this Enum value)
    {
        var field = value.GetType().GetField(value.ToString());
        var attribute = field?.GetCustomAttribute<DescriptionAttribute>();
        return attribute?.Description ?? value.ToString();
    }

    /// <summary>
    /// Boxing-free flag check for <c>[Flags]</c> enums. Named <c>HasFlagFast</c>
    /// (not <c>HasFlag</c>) on purpose: <see cref="Enum"/> already declares an
    /// instance <c>HasFlag(Enum)</c> method, and instance methods always win over
    /// extension methods in C# overload resolution - naming it the same would make
    /// this generic, non-boxing version silently unreachable via <c>value.HasFlag(x)</c>.
    /// </summary>
    public static bool HasFlagFast<TEnum>(this TEnum value, TEnum flag) where TEnum : struct, Enum
    {
        var valueAsLong = Convert.ToInt64(value);
        var flagAsLong = Convert.ToInt64(flag);
        return (valueAsLong & flagAsLong) == flagAsLong;
    }
}
