namespace LCTWorks.Core.Extensions;

public static class StringExtensions
{
    public const string DefaultCommentPrefix = "#";

    /// <summary>
    /// Uppercases the first letter of each whitespace-delimited word.
    /// Returns the same instance if no change is needed.
    /// </summary>
    public static string Capitalize(this string? value)
    {
        if (string.IsNullOrEmpty(value))
            return string.Empty;

        if (!NeedsCapitalization(value))
            return value;

        return string.Create(value.Length, value, static (span, source) =>
        {
            source.AsSpan().CopyTo(span);
            Capitalize(span);
        });
    }

    /// <summary>
    /// Uppercases the first letter of each whitespace-delimited word.
    /// Returns the same instance if no change is needed.
    /// </summary>
    public static void Capitalize(this Span<char> value)
    {
        bool atWordStart = true;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (atWordStart)
                value[i] = char.ToUpperInvariant(c);
            atWordStart = char.IsWhiteSpace(c);
        }
    }

    public static string? EmptyIfNull(this string? value)
                       => value ?? string.Empty;

    public static string? NullIfEmpty(this string? value)
                   => string.IsNullOrWhiteSpace(value) ? null : value;

    /// <summary>
    /// Reads a text and splits it into lines.
    /// </summary>
    /// <param name="text">Text to read</param>
    /// <param name="ignoreEmptyLines">Whether you want to ignore empty lines.</param>
    /// <param name="ignorePrefix">Whether you want to ignore lines starting with certain prefix</param>
    /// <param name="prefixToIgnore">Line's prefix you want to ignore.</param>
    /// <returns></returns>
    public static string[] ToLines(
        this string text,
        bool ignoreEmptyLines = true,
        bool ignorePrefix = false,
        string? prefixToIgnore = null)
    {
        if (string.IsNullOrEmpty(text))
            return [];

        prefixToIgnore ??= DefaultCommentPrefix;
        ReadOnlySpan<char> span = text.AsSpan();
        int len = span.Length;

        // First pass: count valid lines
        int count = 0;
        int pos = 0;
        while (pos < len)
        {
            int lineStart = pos;
            while (pos < len && span[pos] != '\r' && span[pos] != '\n')
                pos++;
            int lineEnd = pos;

            if (pos < len && span[pos] == '\r') pos++;
            if (pos < len && span[pos] == '\n') pos++;

            var line = span.Slice(lineStart, lineEnd - lineStart).Trim();
            if (ignoreEmptyLines && line.Length == 0)
                continue;
            if (ignorePrefix && line.StartsWith(prefixToIgnore, StringComparison.Ordinal))
                continue;
            count++;
        }

        if (count == 0)
            return [];

        string[] result = new string[count];
        int idx = 0;
        pos = 0;
        while (pos < len)
        {
            int lineStart = pos;
            while (pos < len && span[pos] != '\r' && span[pos] != '\n')
                pos++;
            int lineEnd = pos;

            if (pos < len && span[pos] == '\r') pos++;
            if (pos < len && span[pos] == '\n') pos++;

            var line = span[lineStart..lineEnd].Trim();
            if (ignoreEmptyLines && line.Length == 0)
                continue;
            if (ignorePrefix && line.StartsWith(prefixToIgnore, StringComparison.Ordinal))
                continue;
            result[idx++] = line.ToString();
        }

        return result;
    }

    private static bool NeedsCapitalization(ReadOnlySpan<char> value)
    {
        bool atWordStart = true;
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            if (atWordStart && c != char.ToUpperInvariant(c))
                return true;
            atWordStart = char.IsWhiteSpace(c);
        }
        return false;
    }
}