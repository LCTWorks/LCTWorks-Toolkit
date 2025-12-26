namespace LCTWorks.Core.Extensions;

public static class UriExtensions
{
    private static readonly string[] imageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff", ".svg", ".ico"];

    public static bool IsImageUri(this Uri uri)
    {
        if (uri == null)
        {
            return false;
        }
        string path = uri.AbsolutePath.ToLowerInvariant();
        return imageExtensions.Any(path.EndsWith);
    }
}