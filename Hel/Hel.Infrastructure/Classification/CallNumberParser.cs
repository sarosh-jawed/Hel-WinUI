using System.Globalization;
using System.Text.RegularExpressions;

namespace Hel.Infrastructure.Classification;

/// <summary>
/// Helper for normalizing call numbers and extracting Dewey values safely.
/// </summary>
internal static partial class CallNumberParser
{
    [GeneratedRegex(@"^(?<num>\d{1,3}(?:\.\d+)?)", RegexOptions.Compiled)]
    private static partial Regex LeadingDeweyRegex();

    public static string NormalizeForPrefixMatch(string? rawValue)
    {
        return string.IsNullOrWhiteSpace(rawValue)
            ? string.Empty
            : rawValue.Trim();
    }

    public static string NormalizeForDeweyParsing(string? rawValue, IEnumerable<string> stripPrefixes)
    {
        if (string.IsNullOrWhiteSpace(rawValue))
            return string.Empty;

        string value = rawValue.Trim();

        // Strip configured prefixes repeatedly from the beginning.
        // We sort by length descending so REF wins before R.
        var prefixes = stripPrefixes
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Select(p => p.Trim())
            .OrderByDescending(p => p.Length)
            .ToList();

        bool stripped;
        do
        {
            stripped = false;

            foreach (string prefix in prefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value[prefix.Length..].TrimStart();
                    stripped = true;
                    break;
                }
            }
        }
        while (stripped && !string.IsNullOrWhiteSpace(value));

        return value;
    }

    public static bool TryExtractDeweyNumber(
        string? rawValue,
        IEnumerable<string> stripPrefixes,
        out decimal deweyValue)
    {
        deweyValue = 0;

        string normalized = NormalizeForDeweyParsing(rawValue, stripPrefixes);
        if (string.IsNullOrWhiteSpace(normalized))
            return false;

        var match = LeadingDeweyRegex().Match(normalized);
        if (!match.Success)
            return false;

        string numberText = match.Groups["num"].Value;

        return decimal.TryParse(
            numberText,
            NumberStyles.AllowDecimalPoint,
            CultureInfo.InvariantCulture,
            out deweyValue);
    }
}
