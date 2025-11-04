namespace LCTWorks.Core.Helpers;

/// <summary>
/// Checks and throw appropiate exceptions for method arguments.
/// </summary>
internal static class ThrowCheck
{
    public static void FilePath(string filePath, string? argErrorMessage = "File not found", string? paramName = "filePath")
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(argErrorMessage, paramName);
        }
    }

    public static void Null(object? value, string? paramName = null)
    {
        if (value is null)
        {
            var valueName = paramName ?? "value";
            throw new ArgumentNullException(valueName, "Value cannot be null.");
        }
    }

    public static void ReadOnlyCollection<T>(ICollection<T> collection)
    {
        if (collection.IsReadOnly)
        {
            throw new NotSupportedException("The collection is read-only.");
        }
    }

    public static void StringNullOrWhiteSpace(string str, string? argErrorMessage = "Invalid string", string? paramName = "str")
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException(argErrorMessage, paramName);
        }
    }
}