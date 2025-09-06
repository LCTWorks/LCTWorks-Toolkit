using System.Text;

namespace LCTWorks.Common.Helpers
{
    public static class FileHelper
    {
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
}