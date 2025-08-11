namespace LCTWorks.Common.Helpers;

public static class ThrowCheck
{
    public static void CheckFilePath(string filePath, string? argErrorMessage = "File not found", string? paramName = "filePath")
    {
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException(argErrorMessage, paramName);
        }
    }

    public static void CheckString(string str, string? argErrorMessage = "Invalid string", string? paramName = "str")
    {
        if (string.IsNullOrWhiteSpace(str))
        {
            throw new ArgumentException(argErrorMessage, paramName);
        }
    }
}