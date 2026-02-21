using System.Net;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using LCTWorks.Core.Extensions;
using LCTWorks.Web.Extensions;

namespace LCTWorks.Web.Tools;

public partial class WebCrawler
{
    private readonly HtmlDocument _doc;
    private readonly bool _docLoaded;
    private readonly string? _html;

    public HtmlNode RootNode => _doc.DocumentNode;

    public Uri? SourceUrl
    {
        get; private set;
    }

    public static WebCrawler FromHtml(string html, string? sourceUrl = null)
        => new(html)
        {
            SourceUrl = Uri.TryCreate(sourceUrl, UriKind.Absolute, out var uri) ? uri : null
        };

    public static async Task<WebCrawler> FromUrlAsync(string url)
        => new(await GetHtmlFromUrlAsync(url))
        {
            SourceUrl = Uri.TryCreate(url, UriKind.Absolute, out var uri) ? uri : null
        };

    public async Task<HtmlImage[]> GetAllImagesAsync(string[]? excludeExtensions = null, bool validateImages = false)
    {
        if (!IsHtmlLoaded())
        {
            return [];
        }
        excludeExtensions ??= [];

        var rootNode = _doc.DocumentNode;
        var results = new List<HtmlImage>();

        // Get images from <img> tags
        var imgNodes = rootNode.Descendants("img");
        if (imgNodes != null)
        {
            foreach (var node in imgNodes)
            {
                var src = node.GetAttributeValue("src", string.Empty);
                if (string.IsNullOrEmpty(src))
                {
                    continue;
                }

                if (excludeExtensions.Any(ext => src.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var image = await CreateHtmlImageAsync(src, validateImages, node);
                if (image != null)
                {
                    results.Add(image);
                }
            }
        }

        // Get images from style="background-image: url(...)"
        var styledNodes = rootNode.SelectNodes("//*[@style]");
        if (styledNodes != null)
        {
            foreach (var node in styledNodes)
            {
                var style = node.GetAttributeValue("style", string.Empty);
                var match = BackgroundImageUrlRegex().Match(style);
                if (!match.Success)
                {
                    continue;
                }

                var src = match.Groups[1].Value;
                if (string.IsNullOrEmpty(src))
                {
                    continue;
                }

                if (excludeExtensions.Any(ext => src.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                var image = await CreateHtmlImageAsync(src, validateImages, node, isBackground: true);
                if (image != null)
                {
                    results.Add(image);
                }
            }
        }

        return [.. results
            .DistinctBy(x => x.Src)
            .OrderByDescending(x => x.Width ?? 0)];
    }

    public async Task<HtmlMetaTags> GetMetaTagsAsync()
    {
        if (!IsHtmlLoaded())
        {
            return new HtmlMetaTags();
        }

        var metaNodes = _doc.DocumentNode.SelectNodes("//meta");
        var linkNodes = _doc.DocumentNode.SelectNodes("//link");

        // Title
        var title = metaNodes.GetMetaAttributeValue("og:title", "title", "twitter:title");
        if (string.IsNullOrWhiteSpace(title))
        {
            var node = _doc.DocumentNode.SelectSingleNode("//title");
            if (node?.FirstChild != null)
            {
                title = node.FirstChild.InnerHtml;
            }
        }

        // Resolve the base URI from <base href>, canonical, og:url, or the source URL
        var baseUri = GetBaseUri(linkNodes, metaNodes);

        return new HtmlMetaTags
        {
            // Basic Meta Tags
            Title = title,
            Description = metaNodes.GetMetaAttributeValue("description"),
            Keywords = metaNodes.GetMetaAttributeValue("keywords"),
            Author = metaNodes.GetMetaAttributeValue("author"),
            Generator = metaNodes.GetMetaAttributeValue("generator"),
            Charset = _doc.SelectSingleNodeAttribute("//meta[@charset]", "charset"),
            Viewport = metaNodes.GetMetaAttributeValue("viewport"),
            Robots = metaNodes.GetMetaAttributeValue("robots"),
            CanonicalUrl = linkNodes.GetLinkHref("canonical"),
            Language = _doc.SelectSingleNodeAttribute("//html", "lang"),
            ThemeColor = metaNodes.GetMetaAttributeValue("theme-color"),
            ColorScheme = metaNodes.GetMetaAttributeValue("color-scheme"),
            ApplicationName = metaNodes.GetMetaAttributeValue("application-name"),

            // Open Graph Tags
            OgTitle = metaNodes.GetMetaAttributeValue("og:title"),
            OgDescription = metaNodes.GetMetaAttributeValue("og:description"),
            OgType = metaNodes.GetMetaAttributeValue("og:type"),
            OgUrl = metaNodes.GetMetaAttributeValue("og:url"),
            OgImage = metaNodes.GetMetaAttributeValue("og:image"),
            OgImageAlt = metaNodes.GetMetaAttributeValue("og:image:alt"),
            OgImageWidth = metaNodes.GetMetaAttributeValue("og:image:width"),
            OgImageHeight = metaNodes.GetMetaAttributeValue("og:image:height"),
            OgSiteName = metaNodes.GetMetaAttributeValue("og:site_name"),
            OgLocale = metaNodes.GetMetaAttributeValue("og:locale"),
            OgVideo = metaNodes.GetMetaAttributeValue("og:video"),
            OgVideoUrl = metaNodes.GetMetaAttributeValue("og:video:url"),
            VideoType = metaNodes.GetMetaAttributeValue("og:video:type"),

            // Twitter Card Tags
            TwitterCard = metaNodes.GetMetaAttributeValue("twitter:card"),
            TwitterSite = metaNodes.GetMetaAttributeValue("twitter:site"),
            TwitterCreator = metaNodes.GetMetaAttributeValue("twitter:creator"),
            TwitterTitle = metaNodes.GetMetaAttributeValue("twitter:title"),
            TwitterDescription = metaNodes.GetMetaAttributeValue("twitter:description"),
            TwitterImage = metaNodes.GetMetaAttributeValue("twitter:image"),
            TwitterImageAlt = metaNodes.GetMetaAttributeValue("twitter:image:alt"),

            // Article Meta
            ArticleAuthor = metaNodes.GetMetaAttributeValue("article:author"),
            ArticlePublishedTime = metaNodes.GetMetaAttributeValue("article:published_time"),
            ArticleModifiedTime = metaNodes.GetMetaAttributeValue("article:modified_time"),
            ArticleSection = metaNodes.GetMetaAttributeValue("article:section"),
            ArticleTags = metaNodes.GetMetaAttributeValue("article:tag"),

            // Apple/iOS Specific
            AppleMobileWebAppCapable = metaNodes.GetMetaAttributeValue("apple-mobile-web-app-capable"),
            AppleMobileWebAppTitle = metaNodes.GetMetaAttributeValue("apple-mobile-web-app-title"),
            AppleMobileWebAppStatusBarStyle = metaNodes.GetMetaAttributeValue("apple-mobile-web-app-status-bar-style"),
            AppleTouchIcon = ResolveUrl(linkNodes.GetLinkHref("apple-touch-icon"), baseUri),

            // Microsoft Specific
            MsApplicationTileColor = metaNodes.GetMetaAttributeValue("msapplication-TileColor"),
            MsApplicationTileImage = ResolveUrl(metaNodes.GetMetaAttributeValue("msapplication-TileImage"), baseUri),

            // Other Common Tags
            Favicon = ResolveUrl(linkNodes.GetLinkHref("icon", "shortcut icon"), baseUri),
            Copyright = metaNodes.GetMetaAttributeValue("copyright"),
            Rating = metaNodes.GetMetaAttributeValue("rating"),
            Referrer = metaNodes.GetMetaAttributeValue("referrer"),
            FormatDetection = metaNodes.GetMetaAttributeValue("format-detection"),
        };
    }

    public async Task<string?> GetPreviewImageAsync()
    {
        var rootNode = _doc.DocumentNode;
        var metaNodes = rootNode.SelectNodes("//meta");
        var linkNodes = rootNode.SelectNodes("//link");

        var thumbnailUrl = metaNodes.GetMetaAttributeValue("og:image", "og:image:url", "og:image:secure_url", "twitter:image", "twitter:image:src", "lp:image");
        if (string.IsNullOrWhiteSpace(thumbnailUrl))
        {
            thumbnailUrl = linkNodes.GetAttributeValue(
                  new Dictionary<string, string>
                  {
                        { "rel", "preload" },
                        { "as", "image" }
                  }, "href");
        }

        return thumbnailUrl;
    }

    [GeneratedRegex(@"url\(['""]?([^'"")\s]+)['""]?\)", RegexOptions.IgnoreCase)]
    private static partial Regex BackgroundImageUrlRegex();

    private static async Task<HtmlImage?> CreateHtmlImageAsync(string src, bool validateImages, HtmlNode node, bool isBackground = false)
    {
        var formattedSrc = src.StartsWith("//") ? $"https:{src}" : src;
        var uri = new UriString(formattedSrc);

        if (!uri.IsValid)
        {
            return null;
        }

        return validateImages && !await uri.ValidateImageDataAsync()
            ? null
            : new HtmlImage
            {
                Src = uri.Value,
                Alt = isBackground ? null : node.GetAttributeValue("alt", string.Empty).NullIfEmpty(),
                Title = node.GetAttributeValue("title", string.Empty).NullIfEmpty(),
                Width = node.GetAttributeValue("width", 0) is > 0 and var w ? w : TryExtractSizeFromUrl(uri.Value, "w"),
                Height = node.GetAttributeValue("height", 0) is > 0 and var h ? h : TryExtractSizeFromUrl(uri.Value, "h"),
                SrcSet = isBackground ? null : node.GetAttributeValue("srcset", string.Empty).NullIfEmpty(),
                Sizes = isBackground ? null : node.GetAttributeValue("sizes", string.Empty).NullIfEmpty(),
                Loading = isBackground ? null : node.GetAttributeValue("loading", string.Empty).NullIfEmpty(),
            };
    }

    /// <summary>
    /// Resolves a potentially relative URL against the given base URI.
    /// Returns the original value if already absolute or if no base URI is available.
    /// </summary>
    private static string? ResolveUrl(string? url, Uri? baseUri)
    {
        if (string.IsNullOrWhiteSpace(url) || baseUri is null)
        {
            return url;
        }

        // Already absolute
        if (Uri.TryCreate(url, UriKind.Absolute, out _))
        {
            return url;
        }

        // Protocol-relative (//example.com/path)
        if (url.StartsWith("//"))
        {
            return $"{baseUri.Scheme}:{url}";
        }

        // Relative path — resolve against base
        if (Uri.TryCreate(baseUri, url, out var resolved))
        {
            return resolved.AbsoluteUri;
        }

        return url;
    }

    /// <summary>
    /// Tries to extract size from CDN URLs like: s(w:1280,h:720) or /1280x720.
    /// </summary>
    private static int? TryExtractSizeFromUrl(string url, string dimension)
    {
        // Pattern: s(w:1280,h:720)
        var match = UrlSizeRegex().Match(url);
        if (match.Success)
        {
            return dimension == "w"
                ? int.TryParse(match.Groups[1].Value, out var w) ? w : null
                : int.TryParse(match.Groups[2].Value, out var h) ? h : null;
        }

        // Pattern: /1280x720. or _1280x720
        match = UrlSizeRegexAlt().Match(url);
        if (match.Success)
        {
            return dimension == "w"
                ? int.TryParse(match.Groups[1].Value, out var w) ? w : null
                : int.TryParse(match.Groups[2].Value, out var h) ? h : null;
        }

        return null;
    }

    /// <summary>
    /// Determines the base URI for resolving relative URLs by checking
    /// the HTML &lt;base&gt; tag, canonical link, og:url, or the original source URL.
    /// </summary>
    private Uri? GetBaseUri(HtmlNodeCollection? linkNodes, HtmlNodeCollection? metaNodes)
    {
        // 1. <base href="...">
        var baseHref = _doc.DocumentNode.SelectSingleNode("//base")?.GetAttributeValue("href", string.Empty);
        if (!string.IsNullOrWhiteSpace(baseHref) && Uri.TryCreate(baseHref, UriKind.Absolute, out var baseUri))
        {
            return baseUri;
        }

        // 2. Canonical URL
        var canonical = linkNodes.GetLinkHref("canonical");
        if (!string.IsNullOrWhiteSpace(canonical) && Uri.TryCreate(canonical, UriKind.Absolute, out var canonUri))
        {
            return new Uri(canonUri.GetLeftPart(UriPartial.Authority));
        }

        // 3. og:url
        var ogUrl = metaNodes.GetMetaAttributeValue("og:url");
        if (!string.IsNullOrWhiteSpace(ogUrl) && Uri.TryCreate(ogUrl, UriKind.Absolute, out var ogUri))
        {
            return new Uri(ogUri.GetLeftPart(UriPartial.Authority));
        }

        // 4. Original source URL
        if (SourceUrl is not null)
        {
            return new Uri(SourceUrl.GetLeftPart(UriPartial.Authority));
        }

        return null;
    }

    #region Internal;

    internal WebCrawler(string? html, HttpClient? client = null)
    {
        _html = html;
        _doc = new HtmlDocument();
        Client = client ?? HttpTools.Client;
        var decodedHtml = HttpUtility.HtmlDecode(_html);
        var decodedHtml5 = decodedHtml.DecodeHtml5Entities();

        try
        {
            _doc.LoadHtml(decodedHtml5);
            _docLoaded = true;
        }
        catch (Exception)
        {
            _docLoaded = false;
        }
    }

    public HttpClient Client
    {
        get; private set;
    }

    private static async Task<string?> GetHtmlFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.TryAddRefererHeader(url);

            using var response = await HttpTools.Client.SendAsync(request);
            if (!response.IsSuccessStatusCode)
            {
                return default;
            }

            return await response.Content.ReadAsStringAsync();
        }
        catch
        {
            return null;
        }
    }

    [GeneratedRegex(@"s\(w:(\d+),h:(\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlSizeRegex();

    [GeneratedRegex(@"[/_](\d{3,4})x(\d{3,4})[\._]", RegexOptions.IgnoreCase)]
    private static partial Regex UrlSizeRegexAlt();

    private bool IsHtmlLoaded()
        => !string.IsNullOrWhiteSpace(_html) && _docLoaded;

    #endregion Internal;
}