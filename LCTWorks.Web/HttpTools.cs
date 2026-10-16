using System.Net;
using LCTWorks.Web.Extensions;
using LCTWorks.Web.Internal;

namespace LCTWorks.Web;

public static class HttpTools
{
    private static readonly HttpClient _httpClient = CreateHttpClient();

    public static HttpClient Client => _httpClient;

    public static HttpClient CreateHttpClient(HttpMessageHandler? messageHandler = null)
    {
        var handler = messageHandler ?? new SocketsHttpHandler
        {
            AutomaticDecompression = DecompressionMethods.All,
            PooledConnectionLifetime = Constants.PooledConnectionLifetime,
            AllowAutoRedirect = true,
            MaxAutomaticRedirections = 10,
            UseCookies = true,
            CookieContainer = new CookieContainer(),
            ConnectTimeout = TimeSpan.FromSeconds(8),
        };
        var client = new HttpClient(handler)
        {
            Timeout = Constants.HttpClientTimeout,
            // Browsers speak HTTP/2; an HTTP/1.1 request with a Chrome User-Agent is a strong bot signal.
            DefaultRequestVersion = HttpVersion.Version20,
            DefaultVersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
        };

        client.DefaultRequestHeaders.AddBrowserHeaders();

        return client;
    }

    /// <summary>
    /// Creates a GET request that negotiates HTTP/2 (falling back to 1.1).
    /// HttpClient.DefaultRequestVersion only applies to GetAsync-style helpers, not to hand-built
    /// HttpRequestMessage instances, which default to HTTP/1.1.
    /// </summary>
    public static HttpRequestMessage CreateGetRequest(Uri uri)
        => new(HttpMethod.Get, uri)
        {
            Version = HttpVersion.Version20,
            VersionPolicy = HttpVersionPolicy.RequestVersionOrLower,
        };

    /// <inheritdoc cref="CreateGetRequest(Uri)"/>
    public static HttpRequestMessage CreateGetRequest(string url)
        => CreateGetRequest(new Uri(url, UriKind.Absolute));

    public static async Task<byte[]?> DownloadBytesAsync(string? url, HttpMessageHandler? messageHandler = null)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return default;
        }
        using var request = CreateGetRequest(url);
        using var response = await GetClient(messageHandler).SendAsync(request);
        if (!response.IsSuccessStatusCode)
        {
            return default;
        }

        var bytes = await response.Content.ReadAsByteArrayAsync();
        return bytes.Length > 0 ? bytes : null;
    }

    private static HttpClient GetClient(HttpMessageHandler? messageHandler)
    {
        return messageHandler == null ? Client : CreateHttpClient(messageHandler);
    }
}
