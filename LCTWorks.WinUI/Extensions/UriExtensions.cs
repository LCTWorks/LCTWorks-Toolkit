using System;
using System.Threading.Tasks;
using Windows.Storage;

namespace LCTWorks.WinUI.Extensions;

public static class UriExtensions
{
    public static async Task<string?> ReadTextFromApplicationUriAsync(this Uri uri)
    {
        if (uri == null)
        {
            return null;
        }
        string? fileText = null;
        try
        {
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            fileText = await PathIO.ReadTextAsync(file.Path);
        }
        catch
        {
        }
        return fileText;
    }

    public static async Task<string?> ReadTextFromApplicationUriAsync(this string uriPath)
    {
        if (string.IsNullOrWhiteSpace(uriPath))
        {
            return null;
        }
        string? fileText = null;
        try
        {
            Uri uri = new(uriPath);
            var file = await StorageFile.GetFileFromApplicationUriAsync(uri);
            fileText = await PathIO.ReadTextAsync(file.Path);
        }
        catch
        {
        }
        return fileText;
    }
}