using System.Text.RegularExpressions;
using LCTWorks.Web.Tools;

namespace LCTWorks.Web.Internal.Previews;

/// <summary>
/// Amazon product previews. Pages rarely include OG tags and often answer automated clients with a captcha,
/// so this normalizes the URL to /dp/{ASIN}, reads the product title/image from the DOM when the page loads,
/// and falls back to Amazon's legacy ASIN image endpoint when it doesn't.
/// </summary>
/// <remarks>
/// For reliable, terms-compliant product data use Amazon's Product Advertising API (requires an Associates account).
/// The legacy image endpoint is undocumented and best-effort; it works well for books (ISBN-10 ASINs).
/// </remarks>
internal sealed partial class AmazonPreviewProvider : IPreviewProvider
{
    public bool IsAuthoritative => true;

    public string Name => "Amazon";

    public static string? GetAsin(Uri uri)
    {
        var match = AsinRegex().Match(uri.AbsolutePath);
        return match.Success ? match.Groups[1].Value.ToUpperInvariant() : null;
    }

    public bool CanHandle(Uri uri)
            => AmazonHostRegex().IsMatch(uri.Host) || IsShortLink(uri);

    public async Task<WebPreview?> GetPreviewAsync(Uri uri, HttpClient client, CancellationToken cancellationToken)
    {
        if (IsShortLink(uri))
        {
            uri = await PreviewHttp.ResolveRedirectAsync(client, uri, cancellationToken).ConfigureAwait(false) ?? uri;
            if (!AmazonHostRegex().IsMatch(uri.Host))
            {
                return null;
            }
        }

        var asin = GetAsin(uri);
        // /dp/{ASIN} is the canonical, lightest product URL and drops tracking parameters.
        var target = asin != null ? new Uri($"https://{uri.Host}/dp/{asin}") : uri;

        var crawler = await WebCrawler.FromUrlAsync(target.AbsoluteUri, client, retryAsPreviewBot: true, cancellationToken).ConfigureAwait(false);

        string? title = null, description = null, image = null, icon = null;
        if (crawler.IsLoaded)
        {
            title = WebCrawler.Clean(crawler.RootNode.SelectSingleNode("//*[@id='productTitle']")?.InnerText)
                ?? CleanTitle(crawler.GetTitle());
            description = crawler.GetDescription();
            image = crawler.GetPreviewImage(); // handles data-old-hires / data-a-dynamic-image (landingImage, imgBlkFront)
            icon = crawler.GetIconUrl();
        }

        if (image == null && asin != null)
        {
            var legacy = $"https://images-na.ssl-images-amazon.com/images/P/{asin}.01.LZZZZZZZ.jpg";
            // A missing image returns a 1x1 GIF (~43 bytes).
            if (await PreviewHttp.IsRealImageAsync(client, legacy, minBytes: 1024, cancellationToken).ConfigureAwait(false))
            {
                image = legacy;
            }
        }

        return new WebPreview
        {
            Url = uri.AbsoluteUri,
            FinalUrl = target.AbsoluteUri,
            Title = title,
            Description = description,
            SiteName = "Amazon",
            ImageUrl = image,
            IconUrl = icon ?? "https://www.amazon.com/favicon.ico",
            Provider = Name,
            IsBlocked = crawler.IsBlocked && title == null && image == null,
            BlockReason = crawler.BlockReason,
        };
    }

    [GeneratedRegex(@"^(?:www\.|smile\.|m\.)?amazon\.(?:com|ca|com\.mx|com\.br|co\.uk|de|fr|it|es|nl|se|pl|com\.be|com\.tr|ae|sa|eg|in|co\.jp|com\.au|sg|cn)$", RegexOptions.IgnoreCase)]
    private static partial Regex AmazonHostRegex();

    [GeneratedRegex(@"/(?:dp|gp/product|gp/aw/d|exec/obidos/ASIN|o/ASIN|product)/([A-Z0-9]{10})(?:[/?]|$)", RegexOptions.IgnoreCase)]
    private static partial Regex AsinRegex();

    private static string? CleanTitle(string? title)
    {
        if (title == null)
        {
            return null;
        }
        // "Amazon.com: Product Name : Electronics" → "Product Name"
        title = TitlePrefixRegex().Replace(title, string.Empty);
        var suffix = title.IndexOf(" : Amazon.", StringComparison.OrdinalIgnoreCase);
        if (suffix > 0)
        {
            title = title[..suffix];
        }
        return WebCrawler.Clean(title);
    }

    private static bool IsShortLink(Uri uri)
        => uri.Host.ToLowerInvariant() is "amzn.to" or "amzn.eu" or "amzn.asia" or "a.co";

    [GeneratedRegex(@"^Amazon\.[a-z.]+\s*:\s*", RegexOptions.IgnoreCase)]
    private static partial Regex TitlePrefixRegex();
}