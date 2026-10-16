using HtmlAgilityPack;
using LCTWorks.Web.Tools;

namespace LCTWorks.Web.Internal.Previews;

/// <summary>
/// Previews from known public oEmbed endpoints (X/Twitter, Vimeo, TikTok, Spotify, SoundCloud, Flickr, Dailymotion).
/// These sites are JS-heavy or bot-protected, but their oEmbed APIs are public and stable.
/// </summary>
internal sealed class OEmbedPreviewProvider : IPreviewProvider
{
    private readonly List<Endpoint> _endpoints =
    [
        new(["twitter.com", "x.com"], "https://publish.twitter.com/oembed?omit_script=true&dnt=true&url={0}", "X"),
        new(["vimeo.com"], "https://vimeo.com/api/oembed.json?url={0}", "Vimeo"),
        new(["tiktok.com"], "https://www.tiktok.com/oembed?url={0}", "TikTok"),
        new(["open.spotify.com"], "https://open.spotify.com/oembed?url={0}", "Spotify"),
        new(["soundcloud.com"], "https://soundcloud.com/oembed?format=json&url={0}", "SoundCloud"),
        new(["flickr.com", "flic.kr"], "https://www.flickr.com/services/oembed/?format=json&url={0}", "Flickr"),
        new(["dailymotion.com", "dai.ly"], "https://www.dailymotion.com/services/oembed?format=json&url={0}", "Dailymotion"),
    ];

    public bool IsAuthoritative => false;

    public string Name => "oEmbed";

    /// <summary>
    /// Registers an additional oEmbed endpoint.
    /// </summary>
    /// <param name="hosts">Domains handled (subdomains included), e.g. "example.com".</param>
    /// <param name="endpointFormat">Endpoint with {0} as the URL-encoded page URL.</param>
    public OEmbedPreviewProvider AddEndpoint(string[] hosts, string endpointFormat, string? siteName = null)
    {
        _endpoints.Insert(0, new Endpoint(hosts, endpointFormat, siteName));
        return this;
    }

    public bool CanHandle(Uri uri) => Find(uri) != null;

    public async Task<WebPreview?> GetPreviewAsync(Uri uri, HttpClient client, CancellationToken cancellationToken)
    {
        var endpoint = Find(uri);
        if (endpoint == null)
        {
            return null;
        }

        var oembed = await OEmbedResult.FetchAsync(
            client,
            string.Format(System.Globalization.CultureInfo.InvariantCulture, endpoint.Format, Uri.EscapeDataString(uri.AbsoluteUri)),
            cancellationToken).ConfigureAwait(false);
        if (oembed == null)
        {
            return null;
        }

        var title = oembed.Title;
        string? description = null;

        // Tweets have no title/thumbnail in oEmbed; the text is inside the returned <blockquote>.
        if (endpoint.SiteName == "X")
        {
            description = ExtractBlockquoteText(oembed.Html);
            title ??= oembed.AuthorName != null ? $"{oembed.AuthorName} on X" : null;
        }

        return new WebPreview
        {
            Url = uri.AbsoluteUri,
            FinalUrl = uri.AbsoluteUri,
            Title = title,
            Description = description,
            Author = oembed.AuthorName,
            SiteName = oembed.ProviderName ?? endpoint.SiteName,
            ImageUrl = oembed.ThumbnailUrl,
            Provider = Name,
        };
    }

    private static string? ExtractBlockquoteText(string? html)
    {
        if (string.IsNullOrWhiteSpace(html))
        {
            return null;
        }
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        return WebCrawler.Clean(doc.DocumentNode.SelectSingleNode("//blockquote/p")?.InnerText);
    }

    private Endpoint? Find(Uri uri)
        => _endpoints.FirstOrDefault(e => e.Hosts.Any(h => PreviewHttp.IsHostOrSubdomain(uri.Host, h)));

    private sealed record Endpoint(string[] Hosts, string Format, string? SiteName);
}