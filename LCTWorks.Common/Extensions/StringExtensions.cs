﻿namespace LCTWorks.Common.Extensions;

public static class StringExtensions
{
    private const string CommentPrefix = "#";

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

        prefixToIgnore ??= CommentPrefix;
        ReadOnlySpan<char> span = text.AsSpan();
        int len = span.Length;

        // First pass: count valid lines
        int count = 0;
        int pos = 0;
        while (pos < len)
        {
            int lineStart = pos;
            // Find end of line
            while (pos < len && span[pos] != '\r' && span[pos] != '\n')
                pos++;
            int lineEnd = pos;

            // Move past line ending
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

        // Second pass: fill result array
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

            var line = span.Slice(lineStart, lineEnd - lineStart).Trim();
            if (ignoreEmptyLines && line.Length == 0)
                continue;
            if (ignorePrefix && line.StartsWith(prefixToIgnore, StringComparison.Ordinal))
                continue;
            result[idx++] = line.ToString();
        }

        return result;
    }
}