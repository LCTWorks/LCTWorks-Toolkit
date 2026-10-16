using System.Text.Json;
using System.Text.RegularExpressions;
using LCTWorks.Web.Tools;

namespace LCTWorks.Web.Internal.Previews;

/// <summary>
/// Reddit previews via the public JSON API (posts and subreddits), falling back to old.reddit.com,
/// which serves server-rendered HTML with OG tags.
/// </summary>
/// <remarks>
/// Reddit's API rules require a unique, descriptive User-Agent; generic or browser-spoofed ones get
/// throttled or blocked. Requests from cloud/datacenter IPs are frequently refused (403) regardless.
/// </remarks>
internal sealed partial class RedditPreviewProvider(string? userAgent = null) : IPreviewProvider
{
    private const int MaxDescriptionLength = 300;

    public bool IsAuthoritative => true;

    public string Name => "Reddit";

    /// <summary>
    /// User-Agent sent to Reddit. Recommended format: "&lt;platform&gt;:&lt;app id&gt;:&lt;version&gt; (by /u/&lt;username&gt;)".
    /// </summary>
    public string UserAgent { get; } = userAgent ?? "windows:LCTWorks.Web.LinkPreview:1.0 (link preview; +https://github.com/LCTWorks/LCTWorks-Toolkit)";

    public bool CanHandle(Uri uri)
        // redd.it short links only; i.redd.it / v.redd.it / preview.redd.it are media hosts.
        => PreviewHttp.IsHostOrSubdomain(uri.Host, "reddit.com") || uri.Host.Equals("redd.it", StringComparison.OrdinalIgnoreCase);

    public async Task<WebPreview?> GetPreviewAsync(Uri uri, HttpClient client, CancellationToken cancellationToken)
    {
        // Share links (/r/x/s/abc123) and redd.it short links redirect to the real post.
        if (uri.Host.Equals("redd.it", StringComparison.OrdinalIgnoreCase) || ShareLinkRegex().IsMatch(uri.AbsolutePath))
        {
            uri = await PreviewHttp.ResolveRedirectAsync(client, uri, cancellationToken, UserAgent).ConfigureAwait(false) ?? uri;
        }

        WebPreview? preview = null;
        var postMatch = PostIdRegex().Match(uri.AbsolutePath);
        var subredditMatch = SubredditRegex().Match(uri.AbsolutePath);

        if (postMatch.Success)
        {
            preview = await FromPostAsync(uri, postMatch.Groups[1].Value, client, cancellationToken).ConfigureAwait(false);
        }
        else if (subredditMatch.Success)
        {
            preview = await FromSubredditAsync(uri, subredditMatch.Groups[1].Value, client, cancellationToken).ConfigureAwait(false);
        }

        preview ??= await FromOldRedditAsync(uri, client, cancellationToken).ConfigureAwait(false);
        return preview;
    }

    private static string? GetPostImage(JsonElement post)
    {
        // 1. Preview image (raw_json=1 → URLs are not HTML-escaped)
        if (post.GetObject("preview") is { } preview
            && preview.TryGetProperty("images", out var images)
            && images.ValueKind == JsonValueKind.Array
            && images.GetArrayLength() > 0
            && images[0].GetObject("source") is { } source
            && source.GetStringValue("url") is { } previewUrl)
        {
            return previewUrl;
        }

        // 2. Gallery: first item of media_metadata
        if (post.GetBool("is_gallery")
            && post.GetObject("media_metadata") is { } metadata)
        {
            string? firstId = null;
            if (post.GetObject("gallery_data") is { } gallery
                && gallery.TryGetProperty("items", out var items)
                && items.ValueKind == JsonValueKind.Array
                && items.GetArrayLength() > 0)
            {
                firstId = items[0].GetStringValue("media_id");
            }
            foreach (var item in metadata.EnumerateObject())
            {
                if (firstId != null && item.Name != firstId)
                {
                    continue;
                }
                if (item.Value.GetObject("s") is { } s && (s.GetStringValue("u") ?? s.GetStringValue("gif")) is { } galleryUrl)
                {
                    return galleryUrl;
                }
            }
        }

        // 3. Direct image link
        var dest = post.GetStringValue("url_overridden_by_dest") ?? post.GetStringValue("url");
        if (dest != null && Uri.TryCreate(dest, UriKind.Absolute, out var destUri)
            && (destUri.Host is "i.redd.it" or "i.imgur.com"
                || ImageExtensionRegex().IsMatch(destUri.AbsolutePath)))
        {
            return dest;
        }

        // 4. Embedded media (YouTube etc.)
        if (post.GetObject("secure_media") is { } media
            && media.GetObject("oembed") is { } oembed
            && oembed.GetStringValue("thumbnail_url") is { } mediaThumb)
        {
            return mediaThumb;
        }

        // 5. Small thumbnail ("self", "default", "nsfw", "spoiler" are placeholders)
        var thumbnail = post.GetStringValue("thumbnail");
        return thumbnail != null && thumbnail.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? thumbnail : null;
    }

    [GeneratedRegex(@"\.(?:jpe?g|png|gif|webp|avif)$", RegexOptions.IgnoreCase)]
    private static partial Regex ImageExtensionRegex();

    [GeneratedRegex(@"/comments/([a-z0-9]+)", RegexOptions.IgnoreCase)]
    private static partial Regex PostIdRegex();

    [GeneratedRegex(@"^/r/[^/]+/s/[A-Za-z0-9]+", RegexOptions.IgnoreCase)]
    private static partial Regex ShareLinkRegex();

    [GeneratedRegex(@"^/r/([A-Za-z0-9_]+)/?$", RegexOptions.IgnoreCase)]
    private static partial Regex SubredditRegex();

    private static string? Truncate(string? text, int max)
    {
        text = WebCrawler.Clean(text);
        if (text == null || text.Length <= max)
        {
            return text;
        }
        return text[..max].TrimEnd() + "…";
    }

    private async Task<WebPreview?> FromOldRedditAsync(Uri uri, HttpClient client, CancellationToken cancellationToken)
    {
        var builder = new UriBuilder(uri) { Host = "old.reddit.com", Scheme = Uri.UriSchemeHttps, Port = -1 };
        var crawler = await WebCrawler.FromUrlAsync(builder.Uri.AbsoluteUri, client, retryAsPreviewBot: true, cancellationToken).ConfigureAwait(false);
        if (!crawler.IsLoaded)
        {
            return null; // let the generic crawler try www.reddit.com
        }

        var title = crawler.GetTitle();
        var image = crawler.GetPreviewImage();
        if (title == null && image == null)
        {
            return null;
        }

        return new WebPreview
        {
            Url = uri.AbsoluteUri,
            FinalUrl = uri.AbsoluteUri,
            Title = title,
            Description = crawler.GetDescription(),
            SiteName = "Reddit",
            ImageUrl = image,
            IconUrl = "https://www.reddit.com/favicon.ico",
            Provider = Name,
        };
    }

    private async Task<WebPreview?> FromPostAsync(Uri uri, string postId, HttpClient client, CancellationToken cancellationToken)
    {
        using var doc = await PreviewHttp.GetJsonAsync(
            client,
            $"https://www.reddit.com/by_id/t3_{postId}.json?raw_json=1",
            cancellationToken,
            UserAgent).ConfigureAwait(false);

        if (doc == null
            || doc.RootElement.GetObject("data") is not { } data
            || !data.TryGetProperty("children", out var children)
            || children.ValueKind != JsonValueKind.Array
            || children.GetArrayLength() == 0
            || children[0].GetObject("data") is not { } post)
        {
            return null;
        }

        var permalink = post.GetStringValue("permalink");
        var subreddit = post.GetStringValue("subreddit_name_prefixed");
        var author = post.GetStringValue("author");

        return new WebPreview
        {
            Url = uri.AbsoluteUri,
            FinalUrl = permalink != null ? $"https://www.reddit.com{permalink}" : uri.AbsoluteUri,
            Title = WebCrawler.Clean(post.GetStringValue("title")),
            Description = Truncate(post.GetStringValue("selftext"), MaxDescriptionLength) ?? subreddit,
            SiteName = subreddit != null ? $"Reddit · {subreddit}" : "Reddit",
            Author = author != null ? $"u/{author}" : null,
            ImageUrl = GetPostImage(post),
            IconUrl = "https://www.reddit.com/favicon.ico",
            IsNsfw = post.GetBool("over_18"),
            Provider = Name,
        };
    }

    private async Task<WebPreview?> FromSubredditAsync(Uri uri, string subreddit, HttpClient client, CancellationToken cancellationToken)
    {
        using var doc = await PreviewHttp.GetJsonAsync(
            client,
            $"https://www.reddit.com/r/{subreddit}/about.json?raw_json=1",
            cancellationToken,
            UserAgent).ConfigureAwait(false);

        if (doc == null || doc.RootElement.GetObject("data") is not { } data)
        {
            return null;
        }

        var icon = NullIfEmpty(data.GetStringValue("community_icon")) ?? NullIfEmpty(data.GetStringValue("icon_img"));
        var banner = NullIfEmpty(data.GetStringValue("banner_background_image"))
            ?? NullIfEmpty(data.GetStringValue("mobile_banner_image"))
            ?? NullIfEmpty(data.GetStringValue("header_img"));

        return new WebPreview
        {
            Url = uri.AbsoluteUri,
            FinalUrl = $"https://www.reddit.com/r/{subreddit}/",
            Title = WebCrawler.Clean(data.GetStringValue("title")) ?? data.GetStringValue("display_name_prefixed"),
            Description = Truncate(data.GetStringValue("public_description"), MaxDescriptionLength),
            SiteName = data.GetStringValue("display_name_prefixed") is { } prefixed ? $"Reddit · {prefixed}" : "Reddit",
            ImageUrl = icon ?? banner,
            IconUrl = icon ?? "https://www.reddit.com/favicon.ico",
            IsNsfw = data.GetBool("over18"),
            Provider = Name,
        };

        static string? NullIfEmpty(string? value) => string.IsNullOrWhiteSpace(value) ? null : value;
    }
}