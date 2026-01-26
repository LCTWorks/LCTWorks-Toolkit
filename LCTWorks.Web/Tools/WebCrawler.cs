using HtmlAgilityPack;
using LCTWorks.Web.Extensions;
using LCTWorks.Core.Extensions;
using LCTWorks.Web.Internal;
using System.Text.RegularExpressions;
using System.Web;

namespace LCTWorks.Web.Tools;

public partial class WebCrawler
{
    private static readonly string[] excludeIconExtensions = [".svg", ".ico"];
    private static readonly string imgDataPrefix = "data:image/";
    private readonly HtmlDocument _doc;
    private readonly bool _docLoaded;
    private readonly string? _html;

    public static WebCrawler FromHtml(string html)
        => new(html);

    public static async Task<WebCrawler> FromUrlAsync(string url)
        => new(await GetHtmlFromUrlAsync(url));

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
        var imgNodes = rootNode.SelectNodes("//img");
        if (imgNodes != null)
        {
            foreach (var node in imgNodes)
            {
                var src = node.GetAttributeValue("src", string.Empty);
                if (string.IsNullOrEmpty(src))
                    continue;

                if (excludeExtensions.Any(ext => src.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    continue;

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
                    continue;

                var src = match.Groups[1].Value;
                if (string.IsNullOrEmpty(src))
                    continue;

                if (excludeExtensions.Any(ext => src.EndsWith(ext, StringComparison.OrdinalIgnoreCase)))
                    continue;

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
        string title = metaNodes.GetMetaAttributeValue("og:title", "title", "twitter:title");
        if (string.IsNullOrWhiteSpace(title))
        {
            var node = _doc.DocumentNode.SelectSingleNode("//title");
            if (node?.FirstChild != null)
            {
                title = node.FirstChild.InnerHtml;
            }
        }

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
            AppleTouchIcon = linkNodes.GetLinkHref("apple-touch-icon"),

            // Microsoft Specific
            MsApplicationTileColor = metaNodes.GetMetaAttributeValue("msapplication-TileColor"),
            MsApplicationTileImage = metaNodes.GetMetaAttributeValue("msapplication-TileImage"),

            // Other Common Tags
            Favicon = linkNodes.GetLinkHref("icon", "shortcut icon"),
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
            return null;

        if (validateImages && !await uri.ValidateImageDataAsync())
            return null;

        return new HtmlImage
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

    #region Internal

    internal WebCrawler(string? html)
    {
        _html = html;
        _doc = new HtmlDocument();
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

    private static async Task<string?> GetHtmlFromUrlAsync(string url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }
        try
        {
            using var client = new HttpClient(new SocketsHttpHandler { SslOptions = new System.Net.Security.SslClientAuthenticationOptions { EnabledSslProtocols = System.Security.Authentication.SslProtocols.Tls12, } });
            client.Timeout = Constants.HttpClientTimeout;

            client.DefaultRequestHeaders.AddUserAgentHeader(url);

            var response = await client.GetAsync(url);

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

    #endregion Internal
}