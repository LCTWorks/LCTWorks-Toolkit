using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace LCTWorks.Core.Helpers;

public static class FileHelper
{
    public static bool CopyFile(string? filePath, string? destFolderPath, bool overwrite = false)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            return false;
        }
        if (string.IsNullOrWhiteSpace(destFolderPath) || !Directory.Exists(destFolderPath))
        {
            return false;
        }
        var fileName = Path.GetFileName(filePath);
        if (string.IsNullOrWhiteSpace(fileName))
        {
            return false;
        }
        var destFilePath = Path.Combine(destFolderPath, fileName);
        try
        {
            File.Copy(filePath, destFilePath, overwrite);
            return true;
        }
        catch
        {
        }
        return false;
    }

    public static bool DeleteFile(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            return false;
        try
        {
            File.Delete(filePath);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool EnsureFolder(string? folderPath)
    {
        if (string.IsNullOrWhiteSpace(folderPath))
        {
            return false;
        }
        try
        {
            if (!Directory.Exists(folderPath))
            {
                Directory.CreateDirectory(folderPath);
            }
            return true;
        }
        catch
        {
        }
        return false;
    }

    public static bool FileExists(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return false;
        }
        return File.Exists(filePath);
    }

    public static string GetFolderSignature(string? folder)
    {
        if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            return ":0:";

        try
        {
            var entries = new List<(string Name, long Size, long WriteTicks)>();

            foreach (var path in Directory.EnumerateFiles(folder, "*", SearchOption.TopDirectoryOnly))
            {
                try
                {
                    var fi = new FileInfo(path);
                    entries.Add((fi.Name, fi.Length, fi.LastWriteTimeUtc.Ticks));
                }
                catch
                {
                }
            }

            if (entries.Count == 0)
                return ":0:";

            // Ensure deterministic order regardless of filesystem enumeration
            entries.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.OrdinalIgnoreCase));

            using var hasher = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
            Span<byte> buffer8 = stackalloc byte[8];

            foreach (var (Name, Size, WriteTicks) in entries)
            {
                // name (case-insensitive match normalized to upper)
                var nameBytes = Encoding.UTF8.GetBytes(Name.ToUpperInvariant());
                hasher.AppendData(nameBytes);
                hasher.AppendData("|"u8.ToArray());

                // size
                BinaryPrimitives.WriteInt64LittleEndian(buffer8, Size);
                hasher.AppendData(buffer8);

                // last write time (UTC ticks)
                BinaryPrimitives.WriteInt64LittleEndian(buffer8, WriteTicks);
                hasher.AppendData(buffer8);
            }

            var hash = hasher.GetHashAndReset();
            var hex = Convert.ToHexString(hash); // uppercase hex
            return $"{entries.Count}:{hex}";
        }
        catch
        {
            return ":0:";
        }
    }

    /// <summary>
    /// Determines whether the specified <paramref name="path"/> is located within the directory tree rooted at <paramref name="root"/>.
    /// </summary>
    /// <param name="path">An absolute or relative file or directory path to check.</param>
    /// <param name="root">
    /// The root directory path that defines the boundary. If null or whitespace, the method returns false
    /// </param>
    /// <returns>
    /// true if the normalized <paramref name="path"/> starts with the normalized <paramref name="root"/> (i.e., it is under that root);
    /// otherwise, false.
    /// </returns>
    public static bool IsPathUnderRoot(string path, string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        try
        {
            var normalizedRoot = Path.GetFullPath(root);
            if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar) && !normalizedRoot.EndsWith(Path.AltDirectorySeparatorChar))
            {
                normalizedRoot += Path.DirectorySeparatorChar;
            }

            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static bool IsUnder(string path, string? root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return false;
        }

        try
        {
            var normalizedRoot = Path.GetFullPath(root);
            if (!normalizedRoot.EndsWith(Path.DirectorySeparatorChar) && !normalizedRoot.EndsWith(Path.AltDirectorySeparatorChar))
            {
                normalizedRoot += Path.DirectorySeparatorChar;
            }

            var normalizedPath = Path.GetFullPath(path);
            return normalizedPath.StartsWith(normalizedRoot, StringComparison.OrdinalIgnoreCase);
        }
        catch
        {
            return false;
        }
    }

    public static string ReadTextFile(string filePath)
    {
        ThrowCheck.StringNullOrWhiteSpace(filePath, "File path cannot be null or whitespace.", nameof(filePath));
        ThrowCheck.FilePath(filePath, paramName: nameof(filePath));

        try
        {
            using var fs = File.Open(filePath, FileMode.OpenOrCreate);
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            return sr.ReadToEnd();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static async Task<string> ReadTextFileAsync(string filePath)
    {
        ThrowCheck.StringNullOrWhiteSpace(filePath, "File path cannot be null or whitespace.", nameof(filePath));
        ThrowCheck.FilePath(filePath, paramName: nameof(filePath));

        try
        {
            using var fs = File.Open(filePath, FileMode.OpenOrCreate);
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            return await sr.ReadToEndAsync();
        }
        catch (Exception)
        {
            throw;
        }
    }

    public static async Task<string?> TryReadTextFileAsync(string filePath)
    {
        try
        {
            using var fs = File.Open(filePath, FileMode.OpenOrCreate);
            using var sr = new StreamReader(fs, new UTF8Encoding(false));
            return await sr.ReadToEndAsync();
        }
        catch (Exception)
        {
            return default;
        }
    }

    public static bool WriteTextFile(string filePath, string content)
    {
        ThrowCheck.StringNullOrWhiteSpace(filePath, "File path cannot be null or whitespace.", nameof(filePath));
        content ??= string.Empty;
        try
        {
            using var fs = File.Open(filePath, FileMode.Create);
            using var sw = new StreamWriter(fs, new UTF8Encoding(false));
            sw.Write(content);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static async Task<bool> WriteTextFileAsync(string filePath, string content)
    {
        ThrowCheck.StringNullOrWhiteSpace(filePath, "File path cannot be null or whitespace.", nameof(filePath));
        content ??= string.Empty;
        try
        {
            var directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            using var fs = File.Open(filePath, FileMode.Create);
            using var sw = new StreamWriter(fs, new UTF8Encoding(false));
            await sw.WriteAsync(content);
            return true;
        }
        catch (Exception)
        {
            return false;
        }
    }
}