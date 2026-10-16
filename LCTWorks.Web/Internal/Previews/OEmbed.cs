using System.Text.Json;
using LCTWorks.Web.Tools;

namespace LCTWorks.Web.Internal.Previews;

/// <summary>
/// A parsed oEmbed response (https://oembed.com).
/// </summary>
internal sealed record OEmbedResult
{
    public string? Type { get; init; }
    public string? Title { get; init; }
    public string? AuthorName { get; init; }
    public string? AuthorUrl { get; init; }
    public string? ProviderName { get; init; }
    public string? ThumbnailUrl { get; init; }
    public string? Html { get; init; }

    /// <summary>
    /// Fetches and parses an oEmbed JSON endpoint. Returns null on failure.
    /// </summary>
    public static async Task<OEmbedResult?> FetchAsync(HttpClient client, string endpointUrl, CancellationToken cancellationToken = default)
    {
        using var doc = await PreviewHttp.GetJsonAsync(client, endpointUrl, cancellationToken).ConfigureAwait(false);
        if (doc == null || doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }
        var root = doc.RootElement;
        var result = new OEmbedResult
        {
            Type = root.GetStringValue("type"),
            Title = WebCrawler.Clean(root.GetStringValue("title")),
            AuthorName = WebCrawler.Clean(root.GetStringValue("author_name")),
            AuthorUrl = root.GetStringValue("author_url"),
            ProviderName = WebCrawler.Clean(root.GetStringValue("provider_name")),
            ThumbnailUrl = root.GetStringValue("thumbnail_url"),
            Html = root.GetStringValue("html"),
        };
        return result;
    }
}