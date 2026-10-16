using System.Buffers;
using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Web;
using HtmlAgilityPack;
using LCTWorks.Web.Extensions;
using LCTWorks.Web.Internal;

namespace LCTWorks.Web.Tools;

/// <summary>
/// Fetches and parses an HTML document to extract link-preview metadata
/// (title, description, site name, icon and thumbnail).
/// </summary>
public partial class WebCrawler
{
    private static readonly string[] ImageMetaKeys =
    [
        "og:image", "og:image:url", "og:image:secure_url",
        "twitter:image", "twitter:image:src",
        "lp:image", "thumbnailurl", "thumbnail", "image",
    ];

    private static readonly JsonDocumentOptions JsonOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly HtmlDocument _doc;
    private readonly bool _docLoaded;
    private readonly string? _html;

    private Uri? _baseUri;
    private bool _baseUriComputed;
    private List<JsonElement>? _jsonLd;
    private List<LinkTag>? _links;
    private Dictionary<string, string>? _meta;

    static WebCrawler()
    {
        // Enables windows-1252, shift_jis, gb2312, etc. for pages that aren't UTF-8.
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    internal WebCrawler(string? html, HttpClient? client = null)
    {
        _html = html;
        _doc = new HtmlDocument();
        Client = client ?? HttpTools.Client;

        // NOTE: the raw HTML is parsed as-is. Entities are decoded per extracted value (see Clean),
        // decoding the whole document first corrupts attributes such as content="&quot;quoted&quot;".
        if (!string.IsNullOrWhiteSpace(html))
        {
            try
            {
                _doc.LoadHtml(html);
                _docLoaded = true;
            }
            catch (Exception)
            {
                _docLoaded = false;
            }
        }
    }

    /// <summary>
    /// Reason the page was considered blocked (e.g. "Cloudflare challenge"), if any.
    /// </summary>
    public string? BlockReason { get; private set; }

    public HttpClient Client { get; private set; }

    /// <summary>
    /// The response Content-Type media type (e.g. "text/html", "image/jpeg"), when fetched from a URL.
    /// </summary>
    public string? ContentType { get; private set; }

    /// <summary>
    /// True when the site answered with a bot challenge, captcha or consent wall instead of the page.
    /// </summary>
    public bool IsBlocked { get; private set; }

    /// <summary>
    /// True when an HTML document was downloaded and parsed.
    /// </summary>
    public bool IsLoaded => IsHtmlLoaded();

    public HtmlNode RootNode => _doc.DocumentNode;

    /// <summary>
    /// The document URL (the final URL after redirects when fetched with <see cref="FromUrlAsync(string, CancellationToken)"/>).
    /// </summary>
    public Uri? SourceUrl { get; private set; }

    /// <summary>
    /// The HTTP status code of the response, when fetched from a URL.
    /// </summary>
    public HttpStatusCode? StatusCode { get; private set; }

    /// <summary>
    /// The base URI used to resolve relative URLs: &lt;base href&gt;, otherwise the document URL,
    /// otherwise the canonical / og:url.
    /// </summary>
    public Uri? BaseUri
    {
        get
        {
            if (!_baseUriComputed)
            {
                _baseUri = ComputeBaseUri();
                _baseUriComputed = true;
            }
            return _baseUri;
        }
    }

    private List<JsonElement> JsonLd => _jsonLd ??= ParseJsonLd();

    private List<LinkTag> Links => _links ??= BuildLinks();

    private Dictionary<string, string> Meta => _meta ??= BuildMeta();

    #region Factory

    public static WebCrawler FromHtml(string html, string? sourceUrl = null)
        => new(html)
        {
            SourceUrl = TryCreateHttpUri(sourceUrl)
        };

    public static Task<WebCrawler> FromUrlAsync(string url, CancellationToken cancellationToken = default)
        => FromUrlAsync(url, client: null, retryAsPreviewBot: true, cancellationToken);

    /// <summary>
    /// Downloads and parses the given URL.
    /// </summary>
    /// <param name="url">Absolute http(s) URL.</param>
    /// <param name="client">HttpClient to use; defaults to <see cref="HttpTools.Client"/>.</param>
    /// <param name="retryAsPreviewBot">
    /// When the browser-like request is blocked (bot challenge, 403, 503), retry once identifying as a
    /// link-preview bot. Many sites serve a lightweight page with OG tags to preview bots.
    /// </param>
    public static async Task<WebCrawler> FromUrlAsync(string url, HttpClient? client, bool retryAsPreviewBot = true, CancellationToken cancellationToken = default)
    {
        client ??= HttpTools.Client;
        var result = await FetchAsync(client, url, retryAsPreviewBot, cancellationToken).ConfigureAwait(false);
        return new WebCrawler(result.Html, client)
        {
            SourceUrl = result.FinalUrl ?? TryCreateHttpUri(url),
            StatusCode = result.StatusCode,
            IsBlocked = result.IsBlocked,
            BlockReason = result.BlockReason,
            ContentType = result.ContentType,
        };
    }

    #endregion Factory

    #region Preview fields

    public string? GetTitle()
    {
        if (!IsHtmlLoaded())
        {
            return null;
        }
        return GetMeta("og:title", "twitter:title", "title")
            ?? Clean(_doc.DocumentNode.SelectSingleNode("//head/title")?.InnerText)
            ?? Clean(_doc.DocumentNode.SelectSingleNode("//title")?.InnerText)
            ?? GetJsonLdString("headline", "name");
    }

    public string? GetDescription()
    {
        if (!IsHtmlLoaded())
        {
            return null;
        }
        return GetMeta("og:description", "twitter:description", "description")
            ?? GetJsonLdString("description");
    }

    public string? GetSiteName()
    {
        if (!IsHtmlLoaded())
        {
            return null;
        }
        return GetMeta("og:site_name", "application-name", "apple-mobile-web-app-title")
            ?? GetJsonLdPublisherName();
    }

    /// <summary>
    /// Returns the best preview image: OG/Twitter tags, image_src, itemprop, JSON-LD,
    /// e-commerce dynamic images (Amazon), then preloaded images.
    /// </summary>
    public string? GetPreviewImage()
    {
        if (!IsHtmlLoaded())
        {
            return null;
        }

        foreach (var key in ImageMetaKeys)
        {
            var url = ResolveImageUrl(GetMeta(key));
            if (url != null)
            {
                return url;
            }
        }

        var imageSrc = ResolveImageUrl(GetLinkHref("image_src"));
        if (imageSrc != null)
        {
            return imageSrc;
        }

        var jsonLdImage = ResolveImageUrl(GetJsonLdImage());
        if (jsonLdImage != null)
        {
            return jsonLdImage;
        }

        var dynamicImage = ResolveImageUrl(GetDynamicImage());
        if (dynamicImage != null)
        {
            return dynamicImage;
        }

        var preload = Links.FirstOrDefault(l => l.HasRel("preload") && string.Equals(l.As, "image", StringComparison.OrdinalIgnoreCase));
        if (preload != null)
        {
            return ResolveImageUrl(preload.Href) ?? ResolveImageUrl(GetLargestFromSrcSet(preload.ImageSrcSet));
        }

        return null;
    }

    public Task<string?> GetPreviewImageAsync()
        => Task.FromResult(GetPreviewImage());

    /// <summary>
    /// Returns the best site icon: the largest declared icon/apple-touch-icon, otherwise /favicon.ico.
    /// </summary>
    public string? GetIconUrl()
    {
        if (!IsHtmlLoaded())
        {
            return null;
        }

        var best = Links
            .Where(l => l.HasRel("icon") || l.HasRel("apple-touch-icon") || l.HasRel("apple-touch-icon-precomposed"))
            .Select(l => (Url: ResolveUrl(l.Href), Score: ScoreIcon(l)))
            .Where(x => x.Url != null)
            .OrderByDescending(x => x.Score)
            .Select(x => x.Url)
            .FirstOrDefault();

        if (best != null)
        {
            return best;
        }

        return BaseUri is { } baseUri ? new Uri(baseUri, "/favicon.ico").AbsoluteUri : null;
    }

    /// <summary>
    /// The oEmbed JSON endpoint advertised by the page (&lt;link rel="alternate" type="application/json+oembed"&gt;), if any.
    /// </summary>
    public string? GetOEmbedUrl()
    {
        if (!IsHtmlLoaded())
        {
            return null;
        }
        var link = Links.FirstOrDefault(l =>
            l.HasRel("alternate")
            && l.Type != null
            && l.Type.Contains("json+oembed", StringComparison.OrdinalIgnoreCase));
        return link == null ? null : ResolveUrl(link.Href);
    }

    #endregion Preview fields

    #region Meta tags

    public HtmlMetaTags GetMetaTags()
    {
        if (!IsHtmlLoaded())
        {
            return new HtmlMetaTags();
        }

        return new HtmlMetaTags
        {
            // Basic Meta Tags
            Title = GetTitle(),
            Description = GetMeta("description"),
            Keywords = GetMeta("keywords"),
            Author = GetMeta("author"),
            SiteName = GetSiteName(),
            Generator = GetMeta("generator"),
            Charset = Clean(_doc.SelectSingleNodeAttribute("//meta[@charset]", "charset")),
            Viewport = GetMeta("viewport"),
            Robots = GetMeta("robots"),
            CanonicalUrl = ResolveUrl(GetLinkHref("canonical")),
            Language = Clean(_doc.SelectSingleNodeAttribute("//html", "lang")),
            ThemeColor = GetMeta("theme-color"),
            ColorScheme = GetMeta("color-scheme"),
            ApplicationName = GetMeta("application-name"),

            // Open Graph Tags
            OgTitle = GetMeta("og:title"),
            OgDescription = GetMeta("og:description"),
            OgType = GetMeta("og:type"),
            OgUrl = ResolveUrl(GetMeta("og:url")),
            OgImage = ResolveImageUrl(GetMeta("og:image", "og:image:url", "og:image:secure_url")),
            OgImageAlt = GetMeta("og:image:alt"),
            OgImageWidth = GetMeta("og:image:width"),
            OgImageHeight = GetMeta("og:image:height"),
            OgSiteName = GetMeta("og:site_name"),
            OgLocale = GetMeta("og:locale"),
            OgVideo = ResolveUrl(GetMeta("og:video")),
            OgVideoUrl = ResolveUrl(GetMeta("og:video:url", "og:video:secure_url")),
            VideoType = GetMeta("og:video:type"),

            // Twitter Card Tags
            TwitterCard = GetMeta("twitter:card"),
            TwitterSite = GetMeta("twitter:site"),
            TwitterCreator = GetMeta("twitter:creator"),
            TwitterTitle = GetMeta("twitter:title"),
            TwitterDescription = GetMeta("twitter:description"),
            TwitterImage = ResolveImageUrl(GetMeta("twitter:image", "twitter:image:src")),
            TwitterImageAlt = GetMeta("twitter:image:alt"),

            // Article Meta
            ArticleAuthor = GetMeta("article:author"),
            ArticlePublishedTime = GetMeta("article:published_time"),
            ArticleModifiedTime = GetMeta("article:modified_time"),
            ArticleSection = GetMeta("article:section"),
            ArticleTags = GetMeta("article:tag"),

            // Apple/iOS Specific
            AppleMobileWebAppCapable = GetMeta("apple-mobile-web-app-capable"),
            AppleMobileWebAppTitle = GetMeta("apple-mobile-web-app-title"),
            AppleMobileWebAppStatusBarStyle = GetMeta("apple-mobile-web-app-status-bar-style"),
            AppleTouchIcon = ResolveUrl(GetLinkHref("apple-touch-icon", "apple-touch-icon-precomposed")),

            // Microsoft Specific
            MsApplicationTileColor = GetMeta("msapplication-TileColor"),
            MsApplicationTileImage = ResolveUrl(GetMeta("msapplication-TileImage")),

            // Other Common Tags
            Favicon = ResolveUrl(GetLinkHref("icon")),
            Copyright = GetMeta("copyright"),
            Rating = GetMeta("rating"),
            Referrer = GetMeta("referrer"),
            FormatDetection = GetMeta("format-detection"),
        };
    }

    public Task<HtmlMetaTags> GetMetaTagsAsync()
        => Task.FromResult(GetMetaTags());

    /// <summary>
    /// Returns the first non-empty, entity-decoded content of the given meta keys.
    /// Matches property, name, itemprop and http-equiv attributes, case-insensitively.
    /// </summary>
    public string? GetMeta(params string[] keys)
    {
        foreach (var key in keys)
        {
            if (Meta.TryGetValue(key, out var value))
            {
                return value;
            }
        }
        return null;
    }

    #endregion Meta tags

    #region Images

    public async Task<HtmlImage[]> GetAllImagesAsync(string[]? excludeExtensions = null, bool validateImages = false, CancellationToken cancellationToken = default)
    {
        if (!IsHtmlLoaded())
        {
            return [];
        }
        excludeExtensions ??= [];

        var rootNode = _doc.DocumentNode;
        var candidates = new List<HtmlImage>();

        // <img> tags (including lazy-loading attributes)
        foreach (var node in rootNode.Descendants("img"))
        {
            var src = GetImageSource(node);
            var image = CreateHtmlImage(src, node, isBackground: false);
            if (image != null)
            {
                candidates.Add(image);
            }
        }

        // <picture><source srcset>
        foreach (var node in rootNode.Descendants("source"))
        {
            if (node.ParentNode?.Name != "picture")
            {
                continue;
            }
            var src = GetLargestFromSrcSet(node.GetAttributeValue("srcset", string.Empty) is { Length: > 0 } s ? s : node.GetAttributeValue("data-srcset", string.Empty));
            var image = CreateHtmlImage(src, node, isBackground: false);
            if (image != null)
            {
                candidates.Add(image);
            }
        }

        // style="background-image: url(...)"
        var styledNodes = rootNode.SelectNodes("//*[@style]");
        if (styledNodes != null)
        {
            foreach (var node in styledNodes)
            {
                var match = BackgroundImageUrlRegex().Match(node.GetAttributeValue("style", string.Empty));
                if (!match.Success)
                {
                    continue;
                }
                var image = CreateHtmlImage(match.Groups[1].Value, node, isBackground: true);
                if (image != null)
                {
                    candidates.Add(image);
                }
            }
        }

        var filtered = candidates
            .Where(x => !HasExcludedExtension(x.Src!, excludeExtensions))
            .DistinctBy(x => x.Src)
            .ToList();

        if (validateImages && filtered.Count > 0)
        {
            var valid = new ConcurrentDictionary<string, bool>();
            await Parallel.ForEachAsync(
                filtered,
                new ParallelOptions { MaxDegreeOfParallelism = 4, CancellationToken = cancellationToken },
                async (image, token) =>
                {
                    if (await image.Src.IsValidImageDataUrlAsync(Client, token).ConfigureAwait(false))
                    {
                        valid[image.Src!] = true;
                    }
                }).ConfigureAwait(false);
            filtered = [.. filtered.Where(x => valid.ContainsKey(x.Src!))];
        }

        return [.. filtered.OrderByDescending(x => x.Width ?? 0)];
    }

    private static string? GetImageSource(HtmlNode node)
    {
        var src = node.GetAttributeValue("src", string.Empty);
        if (!string.IsNullOrWhiteSpace(src) && !src.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return src;
        }

        // Lazy-loaded images usually keep a placeholder in src and the real URL elsewhere
        foreach (var attr in new[] { "data-src", "data-lazy-src", "data-original", "data-url", "data-hi-res-src" })
        {
            var value = node.GetAttributeValue(attr, string.Empty);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return GetLargestFromSrcSet(node.GetAttributeValue("srcset", string.Empty))
            ?? GetLargestFromSrcSet(node.GetAttributeValue("data-srcset", string.Empty));
    }

    private static bool HasExcludedExtension(string src, string[] excludeExtensions)
    {
        if (excludeExtensions.Length == 0)
        {
            return false;
        }
        var path = Uri.TryCreate(src, UriKind.Absolute, out var uri) ? uri.AbsolutePath : src;
        return excludeExtensions.Any(ext => path.EndsWith(ext, StringComparison.OrdinalIgnoreCase));
    }

    private HtmlImage? CreateHtmlImage(string? src, HtmlNode node, bool isBackground)
    {
        var url = ResolveImageUrl(src);
        if (url == null)
        {
            return null;
        }

        return new HtmlImage
        {
            Src = url,
            Alt = isBackground ? null : Clean(node.GetAttributeValue("alt", string.Empty)),
            Title = Clean(node.GetAttributeValue("title", string.Empty)),
            Width = node.GetAttributeValue("width", 0) is > 0 and var w ? w : TryExtractSizeFromUrl(url, "w"),
            Height = node.GetAttributeValue("height", 0) is > 0 and var h ? h : TryExtractSizeFromUrl(url, "h"),
            SrcSet = isBackground ? null : Clean(node.GetAttributeValue("srcset", string.Empty)),
            Sizes = isBackground ? null : Clean(node.GetAttributeValue("sizes", string.Empty)),
            Loading = isBackground ? null : Clean(node.GetAttributeValue("loading", string.Empty)),
        };
    }

    /// <summary>
    /// E-commerce product images exposed through data attributes (Amazon's data-old-hires / data-a-dynamic-image).
    /// </summary>
    private string? GetDynamicImage()
    {
        var hiRes = _doc.DocumentNode.SelectSingleNode("//img[@data-old-hires and string-length(@data-old-hires) > 0]");
        if (hiRes != null)
        {
            return hiRes.GetAttributeValue("data-old-hires", string.Empty);
        }

        var dynamic = _doc.DocumentNode.SelectSingleNode("//img[@data-a-dynamic-image]");
        if (dynamic == null)
        {
            return null;
        }

        // Value is HTML-encoded JSON: {"https://...jpg":[width,height], ...}
        var json = Clean(dynamic.GetAttributeValue("data-a-dynamic-image", string.Empty));
        if (json == null)
        {
            return null;
        }
        try
        {
            using var doc = JsonDocument.Parse(json, JsonOptions);
            string? best = null;
            long bestArea = -1;
            foreach (var prop in doc.RootElement.EnumerateObject())
            {
                long area = 0;
                if (prop.Value.ValueKind == JsonValueKind.Array && prop.Value.GetArrayLength() >= 2
                    && prop.Value[0].TryGetInt64(out var w) && prop.Value[1].TryGetInt64(out var h))
                {
                    area = w * h;
                }
                if (area > bestArea)
                {
                    bestArea = area;
                    best = prop.Name;
                }
            }
            return best;
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>
    /// Picks the largest candidate from a srcset attribute ("a.jpg 480w, b.jpg 1080w" or "a.jpg 1x, b.jpg 2x").
    /// URLs may contain commas (e.g. CDN transforms), so this follows the HTML spec tokenization.
    /// </summary>
    internal static string? GetLargestFromSrcSet(string? srcset)
    {
        if (string.IsNullOrWhiteSpace(srcset))
        {
            return null;
        }

        string? best = null;
        double bestScore = double.MinValue;
        var i = 0;
        while (i < srcset.Length)
        {
            while (i < srcset.Length && (char.IsWhiteSpace(srcset[i]) || srcset[i] == ','))
            {
                i++;
            }
            if (i >= srcset.Length)
            {
                break;
            }

            var start = i;
            while (i < srcset.Length && !char.IsWhiteSpace(srcset[i]))
            {
                i++;
            }
            var url = srcset[start..i];
            string descriptor = string.Empty;
            if (url.EndsWith(','))
            {
                url = url.TrimEnd(',');
            }
            else
            {
                var descStart = i;
                while (i < srcset.Length && srcset[i] != ',')
                {
                    i++;
                }
                descriptor = srcset[descStart..i].Trim();
            }

            var score = 1d;
            if (descriptor.Length > 1
                && double.TryParse(descriptor[..^1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var n))
            {
                score = descriptor[^1] is 'x' or 'X' ? n * 1000 : n;
            }
            if (url.Length > 0 && score > bestScore)
            {
                bestScore = score;
                best = url;
            }
        }
        return best;
    }

    #endregion Images

    #region URL resolution

    /// <summary>
    /// Decodes and resolves a (possibly relative or protocol-relative) URL against <see cref="BaseUri"/>.
    /// Returns null for empty values or non-http(s) results.
    /// </summary>
    public string? ResolveUrl(string? url) => ResolveUrl(url, BaseUri);

    internal static string? ResolveUrl(string? url, Uri? baseUri)
    {
        url = Clean(url);
        if (url == null)
        {
            return null;
        }

        if (url.StartsWith("//", StringComparison.Ordinal))
        {
            url = $"{baseUri?.Scheme ?? Uri.UriSchemeHttps}:{url}";
        }

        // Note: on Unix "/path" parses as an absolute file:// URI, hence the scheme check.
        if (Uri.TryCreate(url, UriKind.Absolute, out var absolute) && IsHttp(absolute))
        {
            return absolute.AbsoluteUri;
        }

        if (baseUri != null && Uri.TryCreate(baseUri, url, out var resolved) && IsHttp(resolved))
        {
            return resolved.AbsoluteUri;
        }

        return null;
    }

    private string? ResolveImageUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || url.TrimStart().StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }
        return ResolveUrl(url);
    }

    private Uri? ComputeBaseUri()
    {
        var documentUri = SourceUrl
            ?? TryCreateHttpUri(Clean(GetLinkHref("canonical")))
            ?? TryCreateHttpUri(GetMeta("og:url"));

        var baseHref = IsHtmlLoaded()
            ? Clean(_doc.DocumentNode.SelectSingleNode("//base[@href]")?.GetAttributeValue("href", string.Empty))
            : null;

        if (baseHref != null)
        {
            if (TryCreateHttpUri(baseHref) is { } absoluteBase)
            {
                return absoluteBase;
            }
            if (documentUri != null && Uri.TryCreate(documentUri, baseHref, out var relativeBase) && IsHttp(relativeBase))
            {
                return relativeBase;
            }
        }

        return documentUri;
    }

    private static bool IsHttp(Uri uri)
        => uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps;

    private static Uri? TryCreateHttpUri(string? url)
        => !string.IsNullOrWhiteSpace(url) && Uri.TryCreate(url, UriKind.Absolute, out var uri) && IsHttp(uri) ? uri : null;

    #endregion URL resolution

    #region Parsing helpers

    /// <summary>
    /// Decodes HTML entities, collapses whitespace and returns null for empty values.
    /// </summary>
    internal static string? Clean(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }
        // Single pass, so "&amp;lt;" becomes the literal text "&lt;" rather than "<".
        var decoded = value.Contains('&')
            ? EntityRegex().Replace(value, static m =>
            {
                var standard = HttpUtility.HtmlDecode(m.Value);
                return standard != m.Value ? standard : m.Value.DecodeHtml5Entities();
            })
            : value;
        decoded = WhitespaceRegex().Replace(decoded, " ").Trim();
        return decoded.Length == 0 ? null : decoded;
    }

    private Dictionary<string, string> BuildMeta()
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var nodes = IsHtmlLoaded() ? _doc.DocumentNode.SelectNodes("//meta") : null;
        if (nodes == null)
        {
            return dict;
        }

        foreach (var node in nodes)
        {
            var content = Clean(node.GetAttributeValue("content", string.Empty));
            if (content == null)
            {
                continue;
            }
            foreach (var attr in new[] { "property", "name", "itemprop", "http-equiv" })
            {
                var key = node.GetAttributeValue(attr, string.Empty).Trim();
                if (key.Length > 0)
                {
                    // First occurrence wins (e.g. the first og:image is the primary one)
                    dict.TryAdd(key, content);
                }
            }
        }
        return dict;
    }

    private List<LinkTag> BuildLinks()
    {
        var list = new List<LinkTag>();
        var nodes = IsHtmlLoaded() ? _doc.DocumentNode.SelectNodes("//link[@rel]") : null;
        if (nodes == null)
        {
            return list;
        }
        foreach (var node in nodes)
        {
            var rel = node.GetAttributeValue("rel", string.Empty)
                .ToLowerInvariant()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            var href = node.GetAttributeValue("href", string.Empty);
            var imageSrcSet = node.GetAttributeValue("imagesrcset", string.Empty);
            if (rel.Length == 0 || (string.IsNullOrWhiteSpace(href) && string.IsNullOrWhiteSpace(imageSrcSet)))
            {
                continue;
            }
            list.Add(new LinkTag(
                rel,
                href,
                Clean(node.GetAttributeValue("type", string.Empty)),
                Clean(node.GetAttributeValue("sizes", string.Empty)),
                Clean(node.GetAttributeValue("as", string.Empty)),
                imageSrcSet));
        }
        return list;
    }

    private string? GetLinkHref(params string[] rels)
    {
        foreach (var rel in rels)
        {
            var link = Links.FirstOrDefault(l => l.HasRel(rel) && !string.IsNullOrWhiteSpace(l.Href));
            if (link != null)
            {
                return link.Href;
            }
        }
        return null;
    }

    private static int ScoreIcon(LinkTag link)
    {
        var size = 0;
        if (link.Sizes != null)
        {
            if (link.Sizes.Contains("any", StringComparison.OrdinalIgnoreCase))
            {
                size = 256;
            }
            foreach (Match m in IconSizeRegex().Matches(link.Sizes))
            {
                if (int.TryParse(m.Groups[1].Value, out var s))
                {
                    size = Math.Max(size, s);
                }
            }
        }
        if (size == 0)
        {
            size = link.HasRel("apple-touch-icon") || link.HasRel("apple-touch-icon-precomposed") ? 180
                : link.Href.EndsWith(".svg", StringComparison.OrdinalIgnoreCase) || link.Type == "image/svg+xml" ? 256
                : 32;
        }
        return size;
    }

    #endregion Parsing helpers

    #region JSON-LD

    private List<JsonElement> ParseJsonLd()
    {
        var results = new List<JsonElement>();
        var nodes = IsHtmlLoaded() ? _doc.DocumentNode.SelectNodes("//script[@type]") : null;
        if (nodes == null)
        {
            return results;
        }

        foreach (var node in nodes)
        {
            if (!node.GetAttributeValue("type", string.Empty).Contains("ld+json", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var text = node.InnerText;
            if (string.IsNullOrWhiteSpace(text))
            {
                continue;
            }
            try
            {
                using var doc = JsonDocument.Parse(text.Trim(), JsonOptions);
                Flatten(doc.RootElement.Clone(), results);
            }
            catch (JsonException)
            {
                // Malformed JSON-LD is common; ignore it.
            }
        }

        // Main entities (Article, Product, VideoObject...) first; site-level objects last,
        // since their name/image are the site name and logo, not the page's.
        return [.. results.OrderBy(e => IsSiteLevelType(e) ? 1 : 0)];

        static void Flatten(JsonElement element, List<JsonElement> output)
        {
            if (element.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in element.EnumerateArray())
                {
                    Flatten(item, output);
                }
            }
            else if (element.ValueKind == JsonValueKind.Object)
            {
                if (element.TryGetProperty("@graph", out var graph))
                {
                    Flatten(graph, output);
                }
                output.Add(element);
            }
        }
    }

    private static bool IsSiteLevelType(JsonElement element)
        => GetJsonLdTypes(element).Any(t => t is "WebSite" or "Organization" or "BreadcrumbList" or "SearchAction" or "WebPage" or "Person" or "ImageObject");

    private static IEnumerable<string> GetJsonLdTypes(JsonElement element)
    {
        if (!element.TryGetProperty("@type", out var type))
        {
            yield break;
        }
        if (type.ValueKind == JsonValueKind.String)
        {
            yield return type.GetString() ?? string.Empty;
        }
        else if (type.ValueKind == JsonValueKind.Array)
        {
            foreach (var t in type.EnumerateArray())
            {
                if (t.ValueKind == JsonValueKind.String)
                {
                    yield return t.GetString() ?? string.Empty;
                }
            }
        }
    }

    private string? GetJsonLdString(params string[] properties)
    {
        foreach (var element in JsonLd)
        {
            // The name of a WebSite/Organization is the site name, not the page title.
            if (GetJsonLdTypes(element).Any(t => t is "WebSite" or "Organization" or "BreadcrumbList" or "SearchAction"))
            {
                continue;
            }
            foreach (var property in properties)
            {
                if (element.TryGetProperty(property, out var value) && value.ValueKind == JsonValueKind.String
                    && Clean(value.GetString()) is { } text)
                {
                    return text;
                }
            }
        }
        return null;
    }

    private string? GetJsonLdImage()
    {
        foreach (var element in JsonLd)
        {
            foreach (var property in new[] { "image", "thumbnailUrl", "thumbnail", "primaryImageOfPage" })
            {
                if (element.TryGetProperty(property, out var value) && ExtractJsonUrl(value) is { } url)
                {
                    return url;
                }
            }
        }
        return null;
    }

    private string? GetJsonLdPublisherName()
    {
        foreach (var element in JsonLd)
        {
            if (element.TryGetProperty("publisher", out var publisher)
                && publisher.ValueKind == JsonValueKind.Object
                && publisher.TryGetProperty("name", out var name)
                && name.ValueKind == JsonValueKind.String)
            {
                return Clean(name.GetString());
            }
        }
        var site = JsonLd.FirstOrDefault(e => GetJsonLdTypes(e).Contains("WebSite"));
        return site.ValueKind == JsonValueKind.Object && site.TryGetProperty("name", out var siteName) && siteName.ValueKind == JsonValueKind.String
            ? Clean(siteName.GetString())
            : null;
    }

    private static string? ExtractJsonUrl(JsonElement value)
    {
        switch (value.ValueKind)
        {
            case JsonValueKind.String:
                return value.GetString();

            case JsonValueKind.Array:
                foreach (var item in value.EnumerateArray())
                {
                    if (ExtractJsonUrl(item) is { } url)
                    {
                        return url;
                    }
                }
                return null;

            case JsonValueKind.Object:
                foreach (var key in new[] { "url", "contentUrl", "@id" })
                {
                    if (value.TryGetProperty(key, out var inner) && inner.ValueKind == JsonValueKind.String)
                    {
                        return inner.GetString();
                    }
                }
                return null;

            default:
                return null;
        }
    }

    #endregion JSON-LD

    #region Fetching

    private static async Task<FetchResult> FetchAsync(HttpClient client, string url, bool retryAsPreviewBot, CancellationToken cancellationToken)
    {
        var uri = TryCreateHttpUri(url);
        if (uri == null)
        {
            return FetchResult.Failed(null);
        }

        var first = await FetchOnceAsync(client, uri, asPreviewBot: false, cancellationToken).ConfigureAwait(false);

        var shouldRetry = retryAsPreviewBot
            && (first.IsBlocked || first.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.ServiceUnavailable or HttpStatusCode.Unauthorized);
        if (!shouldRetry)
        {
            return first;
        }

        var second = await FetchOnceAsync(client, uri, asPreviewBot: true, cancellationToken).ConfigureAwait(false);
        return second.Html != null && !second.IsBlocked ? second : first;
    }

    private static async Task<FetchResult> FetchOnceAsync(HttpClient client, Uri uri, bool asPreviewBot, CancellationToken cancellationToken)
    {
        using var request = HttpTools.CreateGetRequest(uri);
        if (asPreviewBot)
        {
            // Request-level headers override the client's default ones.
            request.Headers.TryAddWithoutValidation("User-Agent", Constants.PreviewBotUserAgent);
            request.Headers.Accept.ParseAdd("text/html,application/xhtml+xml;q=0.9,*/*;q=0.8");
        }
        else
        {
            // No Referer: a direct navigation from the address bar doesn't send one.
            request.Headers.AddNavigationHeaders();
        }

        try
        {
            using var response = await client
                .SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);

            var finalUri = response.RequestMessage?.RequestUri ?? uri;
            var contentType = response.Content.Headers.ContentType;
            var mediaType = contentType?.MediaType;

            // Direct link to an image/PDF/etc.: nothing to parse, but the caller can use the content type.
            if (mediaType != null && !IsHtmlMediaType(mediaType))
            {
                return new FetchResult(null, finalUri, response.StatusCode, false, null, mediaType);
            }

            if (IsConsentWall(finalUri))
            {
                return new FetchResult(null, finalUri, response.StatusCode, true, "Consent wall", mediaType);
            }

            var bytes = await ReadLimitedAsync(response.Content, Constants.MaxHtmlBytes, cancellationToken).ConfigureAwait(false);
            var html = DecodeHtml(bytes, contentType?.CharSet);

            var blockReason = DetectBlock(response, html);
            if (blockReason != null)
            {
                return new FetchResult(null, finalUri, response.StatusCode, true, blockReason, mediaType);
            }

            // Some sites return 404/410 with perfectly valid OG tags; keep those.
            if (!response.IsSuccessStatusCode && !HasPreviewMetadata(html))
            {
                return new FetchResult(null, finalUri, response.StatusCode, false, null, mediaType);
            }

            return new FetchResult(html, finalUri, response.StatusCode, false, null, mediaType);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            return FetchResult.Failed(uri);
        }
    }

    private static bool IsHtmlMediaType(string mediaType)
        => mediaType.Contains("html", StringComparison.OrdinalIgnoreCase)
        || mediaType.Contains("xml", StringComparison.OrdinalIgnoreCase)
        || mediaType.StartsWith("text/", StringComparison.OrdinalIgnoreCase);

    private static bool IsConsentWall(Uri uri)
        => uri.Host.StartsWith("consent.", StringComparison.OrdinalIgnoreCase)
        && (uri.Host.Contains("google.", StringComparison.OrdinalIgnoreCase) || uri.Host.Contains("youtube.", StringComparison.OrdinalIgnoreCase));

    private static bool HasPreviewMetadata(string html)
        => html.Contains("og:title", StringComparison.OrdinalIgnoreCase)
        || html.Contains("og:image", StringComparison.OrdinalIgnoreCase)
        || html.Contains("twitter:title", StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Detects bot challenges, captchas and WAF block pages, which should not be used as preview content.
    /// </summary>
    internal static string? DetectBlock(HttpResponseMessage response, string html)
    {
        if (response.Headers.TryGetValues("cf-mitigated", out var mitigated)
            && mitigated.Any(v => v.Contains("challenge", StringComparison.OrdinalIgnoreCase)))
        {
            return "Cloudflare challenge";
        }

        var title = TitleRegex().Match(html.Length > 65536 ? html[..65536] : html) is { Success: true } m
            ? Clean(m.Groups[1].Value) ?? string.Empty
            : string.Empty;

        if (title.StartsWith("Just a moment", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Attention Required! | Cloudflare", StringComparison.OrdinalIgnoreCase)
            || title.Contains("Please Wait... | Cloudflare", StringComparison.OrdinalIgnoreCase))
        {
            return "Cloudflare challenge";
        }

        // Amazon serves its captcha with a 200 status.
        var host = response.RequestMessage?.RequestUri?.Host ?? string.Empty;
        if (host.Contains("amazon.", StringComparison.OrdinalIgnoreCase)
            && (html.Contains("/errors/validateCaptcha", StringComparison.OrdinalIgnoreCase)
                || html.Contains("api-services-support@amazon.com", StringComparison.OrdinalIgnoreCase)))
        {
            return "Amazon captcha";
        }

        if (response.IsSuccessStatusCode)
        {
            return null;
        }

        var status = (int)response.StatusCode;
        if (status is not (401 or 403 or 429 or 503))
        {
            return null;
        }

        if (html.Contains("captcha-delivery.com", StringComparison.OrdinalIgnoreCase))
        {
            return "DataDome captcha";
        }
        if (html.Contains("px-captcha", StringComparison.OrdinalIgnoreCase) || html.Contains("perimeterx", StringComparison.OrdinalIgnoreCase))
        {
            return "PerimeterX captcha";
        }
        if (html.Contains("whoa there, pardner", StringComparison.OrdinalIgnoreCase)
            || html.Contains("blocked by network security", StringComparison.OrdinalIgnoreCase))
        {
            return "Reddit block";
        }
        if (html.Contains("Access Denied", StringComparison.OrdinalIgnoreCase) && html.Contains("Reference #", StringComparison.OrdinalIgnoreCase))
        {
            return "Akamai block";
        }
        if (html.Contains("cloudflare", StringComparison.OrdinalIgnoreCase))
        {
            return $"Cloudflare block ({status})";
        }
        if (html.Contains("captcha", StringComparison.OrdinalIgnoreCase))
        {
            return "Captcha";
        }
        return status == 429 ? "Rate limited" : null;
    }

    private static async Task<byte[]> ReadLimitedAsync(HttpContent content, int maxBytes, CancellationToken cancellationToken)
    {
        await using var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
        using var output = new MemoryStream();
        var chunk = ArrayPool<byte>.Shared.Rent(16 * 1024);
        try
        {
            while (output.Length < maxBytes)
            {
                var toRead = (int)Math.Min(chunk.Length, maxBytes - output.Length);
                var read = await stream.ReadAsync(chunk.AsMemory(0, toRead), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                {
                    break;
                }
                output.Write(chunk, 0, read);
            }
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(chunk);
        }
        return output.ToArray();
    }

    /// <summary>
    /// Decodes HTML bytes using BOM, then the Content-Type charset, then &lt;meta charset&gt;, defaulting to UTF-8.
    /// </summary>
    internal static string DecodeHtml(byte[] bytes, string? headerCharset)
    {
        if (bytes.Length == 0)
        {
            return string.Empty;
        }

        if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF)
        {
            return Encoding.UTF8.GetString(bytes, 3, bytes.Length - 3);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFF && bytes[1] == 0xFE)
        {
            return Encoding.Unicode.GetString(bytes, 2, bytes.Length - 2);
        }
        if (bytes.Length >= 2 && bytes[0] == 0xFE && bytes[1] == 0xFF)
        {
            return Encoding.BigEndianUnicode.GetString(bytes, 2, bytes.Length - 2);
        }

        var encoding = TryGetEncoding(headerCharset);
        if (encoding == null)
        {
            var head = Encoding.ASCII.GetString(bytes, 0, Math.Min(bytes.Length, 4096));
            var match = MetaCharsetRegex().Match(head);
            encoding = match.Success ? TryGetEncoding(match.Groups[1].Value) : null;
        }
        return (encoding ?? Encoding.UTF8).GetString(bytes);
    }

    private static Encoding? TryGetEncoding(string? charset)
    {
        if (string.IsNullOrWhiteSpace(charset))
        {
            return null;
        }
        var name = charset.Trim().Trim('"', '\'').ToLowerInvariant();
        name = name switch
        {
            "utf8" => "utf-8",
            // Per the HTML spec, these labels mean windows-1252.
            "iso-8859-1" or "latin1" or "us-ascii" or "ascii" => "windows-1252",
            _ => name,
        };
        try
        {
            return Encoding.GetEncoding(name);
        }
        catch (ArgumentException)
        {
            return null;
        }
    }

    #endregion Fetching

    #region Size extraction

    /// <summary>
    /// Tries to extract size from CDN URLs like: s(w:1280,h:720) or /1280x720.
    /// </summary>
    private static int? TryExtractSizeFromUrl(string url, string dimension)
    {
        var match = UrlSizeRegex().Match(url);
        if (!match.Success)
        {
            match = UrlSizeRegexAlt().Match(url);
        }
        if (!match.Success)
        {
            return null;
        }
        var group = dimension == "w" ? 1 : 2;
        return int.TryParse(match.Groups[group].Value, out var value) ? value : null;
    }

    #endregion Size extraction

    #region Regex

    [GeneratedRegex(@"url\(\s*['""]?([^'"")\s]+)['""]?\s*\)", RegexOptions.IgnoreCase)]
    private static partial Regex BackgroundImageUrlRegex();

    [GeneratedRegex(@"&(?:#[0-9]{1,7}|#[xX][0-9a-fA-F]{1,6}|[A-Za-z][A-Za-z0-9]{1,31});")]
    private static partial Regex EntityRegex();

    [GeneratedRegex(@"(\d+)\s*x\s*\d+", RegexOptions.IgnoreCase)]
    private static partial Regex IconSizeRegex();

    [GeneratedRegex(@"<meta[^>]+charset\s*=\s*[""']?\s*([\w\-:.]+)", RegexOptions.IgnoreCase)]
    private static partial Regex MetaCharsetRegex();

    [GeneratedRegex(@"<title[^>]*>([^<]*)</title>", RegexOptions.IgnoreCase)]
    private static partial Regex TitleRegex();

    [GeneratedRegex(@"s\(w:(\d+),h:(\d+)\)", RegexOptions.IgnoreCase)]
    private static partial Regex UrlSizeRegex();

    [GeneratedRegex(@"[/_](\d{3,4})x(\d{3,4})[\._]", RegexOptions.IgnoreCase)]
    private static partial Regex UrlSizeRegexAlt();

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    #endregion Regex

    private bool IsHtmlLoaded()
        => !string.IsNullOrWhiteSpace(_html) && _docLoaded;

    private sealed record LinkTag(string[] Rel, string Href, string? Type, string? Sizes, string? As, string? ImageSrcSet)
    {
        public bool HasRel(string rel) => Rel.Contains(rel, StringComparer.OrdinalIgnoreCase);
    }

    private sealed record FetchResult(string? Html, Uri? FinalUrl, HttpStatusCode? StatusCode, bool IsBlocked, string? BlockReason, string? ContentType)
    {
        public static FetchResult Failed(Uri? uri) => new(null, uri, null, false, null, null);
    }
}
