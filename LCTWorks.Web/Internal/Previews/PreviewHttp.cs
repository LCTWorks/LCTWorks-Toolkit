using System.Text.Json;

namespace LCTWorks.Web.Internal.Previews;

/// <summary>
/// HTTP and JSON helpers shared by preview providers.
/// </summary>
internal static class PreviewHttp
{
    /// <summary>
    /// GETs a JSON document. Returns null on any non-success status, non-JSON response or error.
    /// </summary>
    public static async Task<JsonDocument?> GetJsonAsync(HttpClient client, string url, CancellationToken cancellationToken, string? userAgent = null)
    {
        try
        {
            using var request = HttpTools.CreateGetRequest(url);
            request.Headers.Accept.ParseAdd("application/json");
            if (userAgent != null)
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            }

            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType != null
                && !mediaType.Contains("json", StringComparison.OrdinalIgnoreCase)
                && !mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            return await JsonDocument.ParseAsync(stream, default, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// Follows redirects (short links, share links) and returns the final URL, without reading the body.
    /// </summary>
    public static async Task<Uri?> ResolveRedirectAsync(HttpClient client, Uri uri, CancellationToken cancellationToken, string? userAgent = null)
    {
        try
        {
            using var request = HttpTools.CreateGetRequest(uri);
            if (userAgent != null)
            {
                request.Headers.TryAddWithoutValidation("User-Agent", userAgent);
            }
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            return response.RequestMessage?.RequestUri;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return null;
        }
    }

    /// <summary>
    /// True when the URL returns a successful image response larger than <paramref name="minBytes"/>
    /// (filters out 1x1 / tiny placeholder images returned instead of a 404).
    /// </summary>
    public static async Task<bool> IsRealImageAsync(HttpClient client, string url, int minBytes, CancellationToken cancellationToken)
    {
        try
        {
            using var request = HttpTools.CreateGetRequest(url);
            request.Headers.Accept.ParseAdd("image/avif,image/webp,image/apng,image/*,*/*;q=0.8");
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
            if (!response.IsSuccessStatusCode)
            {
                return false;
            }
            var mediaType = response.Content.Headers.ContentType?.MediaType;
            if (mediaType == null || !mediaType.StartsWith("image/", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }
            if (response.Content.Headers.ContentLength is long length)
            {
                return length > minBytes;
            }
            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            var buffer = new byte[minBytes + 1];
            var read = await stream.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken).ConfigureAwait(false);
            return read > minBytes;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return false;
        }
    }

    public static string? GetStringValue(this JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.String
            ? value.GetString()
            : null;

    public static JsonElement? GetObject(this JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.Object
            ? value
            : null;

    public static bool GetBool(this JsonElement element, string property)
        => element.ValueKind == JsonValueKind.Object
            && element.TryGetProperty(property, out var value)
            && value.ValueKind == JsonValueKind.True;

    public static string? NormalizeHost(string host)
    {
        if (string.IsNullOrWhiteSpace(host))
        {
            return null;
        }
        host = host.ToLowerInvariant();
        foreach (var prefix in new[] { "www.", "m.", "mobile." })
        {
            if (host.StartsWith(prefix, StringComparison.Ordinal))
            {
                return host[prefix.Length..];
            }
        }
        return host;
    }

    public static bool IsHostOrSubdomain(string host, string domain)
        => host.Equals(domain, StringComparison.OrdinalIgnoreCase)
        || host.EndsWith("." + domain, StringComparison.OrdinalIgnoreCase);
}
