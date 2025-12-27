using LCTWorks.Web.Internal;

namespace LCTWorks.Web.Extensions;

public static class UriStringExtensions
{
    private static readonly string[] imageExtensions = [".png", ".jpg", ".jpeg", ".gif", ".bmp", ".webp", ".tiff", ".svg", ".ico"];

    extension(UriString uri)
    {
        /// <summary>
        /// Checks if the URI has an image file extension.
        /// </summary>
        /// <returns></returns>
        public bool IsImageFromExtension()
        {
            if (uri == null)
            {
                return false;
            }
            string path = uri.Value;
            return imageExtensions.Any(path.EndsWith);
        }

        /// <summary>
        /// Validates if the URI points to an actual image by checking the content type and magic bytes.
        /// </summary>
        /// <returns></returns>
        public async Task<bool> ValidateImageDataAsync()
        {
            if (uri == null)
            {
                return false;
            }
            if (!uri.IsImageFromExtension())
            {
                return false;
            }
            try
            {
                var url = uri.Value;
                using var client = new HttpClient
                {
                    Timeout = Constants.HttpClientTimeout
                };
                client.DefaultRequestHeaders.AddUserAgentHeader(url);

                var response = await client.GetAsync(url, HttpCompletionOption.ResponseHeadersRead);

                if (!response.IsSuccessStatusCode)
                {
                    return false;
                }

                var contentType = response.Content.Headers.ContentType?.MediaType;
                if (contentType != null && contentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
                {
                    //Magic bytes check
                    using var stream = await response.Content.ReadAsStreamAsync();
                    var buffer = new byte[12];
                    var bytesRead = await stream.ReadAsync(buffer);

                    if (bytesRead >= 2)
                    {
                        // PNG: 89 50 4E 47
                        if (buffer[0] == 0x89 && buffer[1] == 0x50 && bytesRead >= 4 && buffer[2] == 0x4E && buffer[3] == 0x47)
                        {
                            return true;
                        }
                        // JPEG: FF D8 FF
                        if (buffer[0] == 0xFF && buffer[1] == 0xD8 && bytesRead >= 3 && buffer[2] == 0xFF)
                        {
                            return true;
                        }
                        // GIF: 47 49 46
                        if (buffer[0] == 0x47 && buffer[1] == 0x49 && bytesRead >= 3 && buffer[2] == 0x46)
                        {
                            return true;
                        }
                        // BMP: 42 4D
                        if (buffer[0] == 0x42 && buffer[1] == 0x4D)
                        {
                            return true;
                        }
                        // WebP: 52 49 46 46 ... 57 45 42 50
                        if (bytesRead >= 12 && buffer[0] == 0x52 && buffer[1] == 0x49 && buffer[2] == 0x46 && buffer[3] == 0x46 &&
                            buffer[8] == 0x57 && buffer[9] == 0x45 && buffer[10] == 0x42 && buffer[11] == 0x50)
                        {
                            return true;
                        }
                        // ICO: 00 00 01 00
                        if (bytesRead >= 4 && buffer[0] == 0x00 && buffer[1] == 0x00 && buffer[2] == 0x01 && buffer[3] == 0x00)
                        {
                            return true;
                        }
                        // TIFF: 49 49 2A 00 (little-endian) or 4D 4D 00 2A (big-endian)
                        if (bytesRead >= 4 &&
                            ((buffer[0] == 0x49 && buffer[1] == 0x49 && buffer[2] == 0x2A && buffer[3] == 0x00) ||
                             (buffer[0] == 0x4D && buffer[1] == 0x4D && buffer[2] == 0x00 && buffer[3] == 0x2A)))
                        {
                            return true;
                        }
                        // SVG: < or <? (XML-based, starts with < character)
                        if (buffer[0] == 0x3C)
                        {
                            return contentType == "image/svg+xml";
                        }
                    }
                }

                return false;
            }
            catch
            {
                return false;
            }
        }
    }
}