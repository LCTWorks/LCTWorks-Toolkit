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
            PooledConnectionLifetime = Constants.HttpClientTimeout
        };
        var client = new HttpClient(handler);

        client.DefaultRequestHeaders.AddBrowserHeaders();

        return client;
    }

    public static async Task<byte[]?> DownloadBytesAsync(string? url, HttpMessageHandler? messageHandler = null)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.IsWellFormedUriString(url, UriKind.Absolute))
        {
            return default;
        }
        using var request = new HttpRequestMessage(HttpMethod.Get, url);
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