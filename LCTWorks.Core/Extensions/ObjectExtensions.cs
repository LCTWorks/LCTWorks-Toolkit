namespace LCTWorks.Core.Extensions;

public static class ObjectExtensions
{
    public static string ToLowerInvariantString(this object value)
        => ToStringInternal(value).ToLowerInvariant();

    public static string ToUpperInvariantString(this object value)
    => ToStringInternal(value).ToUpperInvariant();

    private static string ToStringInternal(object value)
        => value?.ToString() ?? string.Empty;
}