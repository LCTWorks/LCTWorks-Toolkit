using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using Windows.Storage;
using Windows.Storage.AccessCache;

namespace LCTWorks.WinUI.Helpers;

public static class AppStorageHelper
{
    public static string? AddToFutureAccessList(StorageFile storageFile, string? token = null)
    {
        token ??= string.Empty;
        if (!StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
        {
            return StorageApplicationPermissions.FutureAccessList.Add(storageFile, token);
        }

        return default;
    }

    public static async Task DeleteFileFromLocalFolderAsync(string? folderName, string? fileName)
    {
        var file = await GetStorageFileFromLocalFolderAsync(folderName, fileName);
        if (file != null)
        {
            await file.DeleteAsync();
        }
    }

    public static async Task<StorageFolder?> GetFutureAccessListFolderAsync(string token)
    {
        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
        {
            return await StorageApplicationPermissions.FutureAccessList.GetFolderAsync(token);
        }
        return default;
    }

    public static StorageFolder GetLocalFolder()
                => ApplicationData.Current.LocalFolder;

    public static StorageFolder GetLocalFolder(string subFolderName)
        => ApplicationData.Current.LocalFolder.CreateFolderAsync(subFolderName, CreationCollisionOption.OpenIfExists).AsTask().GetAwaiter().GetResult();

    public static async Task<StorageFile?> GetStorageFileFromLocalFolderAsync(string? folderName, string? fileName)
    {
        if (string.IsNullOrEmpty(folderName) || string.IsNullOrEmpty(fileName))
        {
            return default;
        }
        try
        {
            var sf = await GetLocalFolder().CreateFolderAsync(folderName, CreationCollisionOption.OpenIfExists);
            return await sf.CreateFileAsync(fileName, CreationCollisionOption.OpenIfExists);
        }
        catch
        {
            return null;
        }
    }

    public static async Task<string> ReadTextOnStorageFileAsync(string? folderName, string? fileName)
    {
        var file = await GetStorageFileFromLocalFolderAsync(folderName, fileName);
        if (file != null)
        {
            return await ReadTextOnStorageFileAsync(file);
        }
        return string.Empty;
    }

    public static async Task<string> ReadTextOnStorageFileAsync(StorageFile? storageFile)
    {
        if (storageFile == null)
        {
            return string.Empty;
        }
        try
        {
            return await FileIO.ReadTextAsync(storageFile);
        }
        catch (Exception)
        {
            return string.Empty;
        }
    }

    public static bool RemoveFromFutureAccessList(string token)
    {
        if (StorageApplicationPermissions.FutureAccessList.ContainsItem(token))
        {
            StorageApplicationPermissions.FutureAccessList.Remove(token);
            return true;
        }
        return false;
    }

    public static async Task<StorageFile?> WriteToLocalFolderFileAsync(string subFolder, string fileName, byte[] content)
    {
        if (string.IsNullOrWhiteSpace(fileName) || content == null || content.Length == 0)
        {
            return null;
        }
        var file = await GetStorageFileFromLocalFolderAsync(subFolder, fileName);
        if (file == null)
        {
            return default;
        }
        await FileIO.WriteBytesAsync(file, content);
        return file;
    }
}