namespace LCTWorks.Core.Helpers;

public static class TextFormatHelper
{
    public static string ToFileSizeString(ulong bytes)
    {
        if (bytes < 0)
        {
            return "N/A";
        }

        if (bytes == 0)
        {
            return "0 B";
        }

        string[] sizes = { "B", "KB", "MB", "GB", "TB", "PB", "EB" };
        double len = bytes;
        var order = 0;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return $"{len:0.##} {sizes[order]}";
    }

    public static string ToLowerFilePath(string path)
    {
        if (string.IsNullOrEmpty(path))
        {
            return path;
        }

        return path[0] + path[1..].ToLowerInvariant();
    }
}