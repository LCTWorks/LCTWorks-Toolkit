using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace LCTWorks.Workshop.Internal;

/// <summary>
/// Converts user-typed glyph notations into the actual characters, so demo pages
/// accept the same text a developer would paste from C# or XAML.
/// </summary>
/// <remarks>
/// Supported notations (may be mixed with literal text):
/// <list type="bullet">
/// <item><c></c> and <c>\U0001F600</c> (C# escapes)</item>
/// <item><c>&amp;#xE705;</c> and <c>&amp;#59141;</c> (XAML / XML character references)</item>
/// <item><c>U+E715</c> (Unicode notation)</item>
/// </list>
/// Anything that doesn't match is left untouched.
/// </remarks>
internal static partial class GlyphParser
{
    public static string Parse(string? input)
    {
        if (string.IsNullOrEmpty(input))
        {
            return string.Empty;
        }

        return GlyphNotationRegex().Replace(input, static match =>
        {
            string? hex = null;
            string? dec = null;

            if (match.Groups["u16"].Success)
            {
                hex = match.Groups["u16"].Value;
            }
            else if (match.Groups["u32"].Success)
            {
                hex = match.Groups["u32"].Value;
            }
            else if (match.Groups["xmlHex"].Success)
            {
                hex = match.Groups["xmlHex"].Value;
            }
            else if (match.Groups["xmlDec"].Success)
            {
                dec = match.Groups["xmlDec"].Value;
            }
            else if (match.Groups["uplus"].Success)
            {
                hex = match.Groups["uplus"].Value;
            }

            int codePoint;
            if (hex is not null)
            {
                if (!int.TryParse(hex, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out codePoint))
                {
                    return match.Value;
                }
            }
            else if (dec is not null)
            {
                if (!int.TryParse(dec, NumberStyles.Integer, CultureInfo.InvariantCulture, out codePoint))
                {
                    return match.Value;
                }
            }
            else
            {
                return match.Value;
            }

            try
            {
                return char.ConvertFromUtf32(codePoint);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Lone surrogates or out-of-range values: fall back to a raw UTF-16 unit when possible.
                return codePoint is >= 0 and <= 0xFFFF ? ((char)codePoint).ToString() : match.Value;
            }
        });
    }

    [GeneratedRegex(
        @"\\u(?<u16>[0-9A-Fa-f]{4})" +
        @"|\\U(?<u32>[0-9A-Fa-f]{8})" +
        @"|&#[xX](?<xmlHex>[0-9A-Fa-f]{1,6});" +
        @"|&#(?<xmlDec>[0-9]{1,7});" +
        @"|U\+(?<uplus>[0-9A-Fa-f]{4,6})",
        RegexOptions.CultureInvariant)]
    private static partial Regex GlyphNotationRegex();
}
