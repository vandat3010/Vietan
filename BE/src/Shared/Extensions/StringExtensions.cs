using System.Text;
using System.Text.RegularExpressions;
using Backend.Shared.Constants;
using Ganss.Xss;

namespace Backend.Shared.Extensions;

/// <summary>Reusable string helpers used across every layer.</summary>
public static partial class StringExtensions
{
    /// <summary>True when the string is null, empty, or made only of whitespace.</summary>
    public static bool IsNullOrEmpty(this string? value) => string.IsNullOrWhiteSpace(value);

    /// <summary>Inverse of <see cref="IsNullOrEmpty"/> - reads better at call sites (<c>if (name.HasValue())</c>).</summary>
    public static bool HasValue(this string? value) => !string.IsNullOrWhiteSpace(value);

    /// <summary>
    /// Identifier / key (username, code): ASCII letters, digits, <c>.</c>, <c>_</c>, <c>-</c>.
    /// Null/empty is not an identifier. Not for Vietnamese free-text.
    /// </summary>
    public static bool IsSafeIdentifier(this string? value) =>
        !string.IsNullOrEmpty(value) && IdentifierRegex().IsMatch(value);

    /// <summary>
    /// True when the value contains C0/C1 control characters that should not appear
    /// in user-facing text. Newline/tab are allowed only when
    /// <paramref name="allowNewLineAndTab"/> is true (multiline Description).
    /// Null/empty has no disallowed characters.
    /// </summary>
    public static bool ContainsDisallowedControlChars(this string? value, bool allowNewLineAndTab = false)
    {
        if (string.IsNullOrEmpty(value))
            return false;

        foreach (var c in value)
        {
            if (!char.IsControl(c))
                continue;

            if (allowNewLineAndTab && c is '\n' or '\r' or '\t')
                continue;

            return true;
        }

        return false;
    }

    /// <summary>
    /// HTML sanitizer for fields that are actually stored/rendered as HTML.
    /// Null-safe: null stays null, empty stays empty. Does <b>not</b> encode plain text —
    /// do not run this on Description/Name that are stored as plain text
    /// (a literal <c>&lt;</c> in "x &lt; 10" would be altered).
    /// Allowed tags: p, br, strong, em, b, i, ul, ol, li. No script/iframe/javascript: URLs.
    /// </summary>
    public static string? SanitizeHtml(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return value;

        return HtmlSanitizerHolder.Instance.Sanitize(value);
    }

    /// <summary>Converts "PascalCase"/"camelCase" to "snake_case" (e.g. for DB columns or query params).</summary>
    public static string ToSnakeCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        var withUnderscores = SnakeCaseRegex().Replace(value, "$1_$2");
        return withUnderscores.ToLowerInvariant();
    }

    /// <summary>Converts "PascalCase"/"snake_case" to "camelCase" (e.g. for JSON payloads).</summary>
    public static string ToCamelCase(this string value)
    {
        if (string.IsNullOrEmpty(value)) return value;

        if (value.Contains('_'))
        {
            var parts = value.Split('_', StringSplitOptions.RemoveEmptyEntries);
            var builder = new StringBuilder(parts[0].ToLowerInvariant());
            for (var i = 1; i < parts.Length; i++)
                builder.Append(char.ToUpperInvariant(parts[i][0])).Append(parts[i][1..]);

            return builder.ToString();
        }

        return char.ToLowerInvariant(value[0]) + value[1..];
    }

    [GeneratedRegex("([a-z0-9])([A-Z])")]
    private static partial Regex SnakeCaseRegex();

    [GeneratedRegex(ValidationConstants.IdentifierPattern)]
    private static partial Regex IdentifierRegex();

    private static class HtmlSanitizerHolder
    {
        internal static readonly HtmlSanitizer Instance = Create();

        private static HtmlSanitizer Create()
        {
            var sanitizer = new HtmlSanitizer();
            sanitizer.AllowedTags.Clear();
            foreach (var tag in new[] { "p", "br", "strong", "em", "b", "i", "ul", "ol", "li" })
                sanitizer.AllowedTags.Add(tag);

            sanitizer.AllowedAttributes.Clear();
            sanitizer.AllowedCssProperties.Clear();
            sanitizer.AllowedAtRules.Clear();
            sanitizer.AllowedClasses.Clear();
            sanitizer.AllowedSchemes.Clear();
            return sanitizer;
        }
    }
}
