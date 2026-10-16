using System.Collections.Concurrent;
using LCTWorks.Web.Internal.Previews;
using LCTWorks.Web.Tools;

namespace LCTWorks.Web;

/// <summary>
/// Builds link previews: site-specific providers first (official APIs / oEmbed), then the generic
/// HTML crawler (OG, Twitter, JSON-LD, oEmbed discovery), then graceful fallbacks (site name + icon).
/// </summary>
public sealed class WebPreviewer
{
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new(StringComparer.Ordinal);
    private readonly HttpClient _client;
    private readonly WebPreviewerOptions _options;

    public WebPreviewer(WebPreviewerOptions? options = null)
    {
        _options = options ?? new WebPreviewerOptions();
        _client = _options.Client ?? HttpTools.Client;
    }

    public static WebPreviewer Default { get; } = new();

    public void ClearCache() => _cache.Clear();

    public async Task<WebPreview> GetPreviewAsync(string url, CancellationToken cancellationToken = default)
    {
        var uri = Normalize(url);
        if (uri == null)
        {
            return new WebPreview { Url = url };
        }

        var key = uri.AbsoluteUri;
        if (_cache.TryGetValue(key, out var cached))
        {
            if (cached.Expires > DateTimeOffset.UtcNow)
            {
                return cached.Preview;
            }
            _cache.TryRemove(key, out _);
        }

        var preview = await BuildAsync(uri, url, cancellationToken).ConfigureAwait(false);
        AddToCache(key, preview);
        return preview;
    }

    internal static WebPreview FromCrawler(string url, WebCrawler crawler, string provider)
    {
        var finalUrl = crawler.SourceUrl?.AbsoluteUri;

        // Direct link to an image
        if (crawler.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) == true)
        {
            return new WebPreview
            {
                Url = url,
                FinalUrl = finalUrl,
                Title = crawler.SourceUrl is { } source ? Uri.UnescapeDataString(Path.GetFileName(source.AbsolutePath)) : null,
                ImageUrl = finalUrl ?? url,
                Provider = provider,
            };
        }

        return new WebPreview
        {
            Url = url,
            FinalUrl = crawler.ResolveUrl(crawler.GetMeta("og:url")) ?? finalUrl,
            Title = crawler.GetTitle(),
            Description = crawler.GetDescription(),
            SiteName = crawler.GetSiteName(),
            Author = crawler.GetMeta("author", "article:author", "twitter:creator"),
            ImageUrl = crawler.GetPreviewImage(),
            IconUrl = crawler.GetIconUrl(),
            VideoUrl = crawler.ResolveUrl(crawler.GetMeta("og:video:secure_url", "og:video:url", "og:video")),
            Provider = provider,
            IsBlocked = crawler.IsBlocked,
            BlockReason = crawler.BlockReason,
        };
    }

    private static Uri? Normalize(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return null;
        }
        url = url.Trim();
        if (!url.Contains("://", StringComparison.Ordinal))
        {
            url = "https://" + url.TrimStart('/');
        }
        return Uri.TryCreate(url, UriKind.Absolute, out var uri)
            && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
            ? uri
            : null;
    }

    private void AddToCache(string key, WebPreview preview)
    {
        var duration = preview.IsBlocked || preview.IsEmpty ? _options.FailedCacheDuration : _options.CacheDuration;
        if (duration <= TimeSpan.Zero || _options.MaxCacheEntries <= 0)
        {
            return;
        }

        if (_cache.Count >= _options.MaxCacheEntries)
        {
            var now = DateTimeOffset.UtcNow;
            foreach (var entry in _cache)
            {
                if (entry.Value.Expires <= now)
                {
                    _cache.TryRemove(entry.Key, out _);
                }
            }
            if (_cache.Count >= _options.MaxCacheEntries)
            {
                _cache.Clear();
            }
        }

        _cache[key] = new CacheEntry(DateTimeOffset.UtcNow + duration, preview);
    }

    private async Task<WebPreview> BuildAsync(Uri uri, string originalUrl, CancellationToken cancellationToken)
    {
        // 1. Site-specific providers
        WebPreview? providerPreview = null;
        foreach (var provider in _options.Providers)
        {
            if (!provider.CanHandle(uri))
            {
                continue;
            }
            try
            {
                providerPreview = await provider.GetPreviewAsync(uri, _client, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                providerPreview = null;
            }

            if (providerPreview != null)
            {
                providerPreview = providerPreview with { Url = originalUrl };
                if (provider.IsAuthoritative || providerPreview.IsComplete)
                {
                    return Finalize(providerPreview, uri);
                }
                break;
            }
        }

        // 2. Generic HTML crawl
        var crawler = await WebCrawler.FromUrlAsync(uri.AbsoluteUri, _client, _options.RetryAsPreviewBot, cancellationToken).ConfigureAwait(false);

        if (crawler.IsBlocked && _options.BlockedPageFetcher != null)
        {
            try
            {
                var html = await _options.BlockedPageFetcher(uri, cancellationToken).ConfigureAwait(false);
                if (!string.IsNullOrWhiteSpace(html))
                {
                    crawler = WebCrawler.FromHtml(html, (crawler.SourceUrl ?? uri).AbsoluteUri);
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
                // Keep the blocked result.
            }
        }

        var htmlPreview = FromCrawler(originalUrl, crawler, "html");

        // 3. oEmbed discovery for anything still missing
        if (_options.UseDiscoveredOEmbed
            && (htmlPreview.Title == null || htmlPreview.ImageUrl == null)
            && crawler.GetOEmbedUrl() is { } oembedUrl)
        {
            var oembed = await OEmbedResult.FetchAsync(_client, oembedUrl, cancellationToken).ConfigureAwait(false);
            if (oembed != null)
            {
                htmlPreview = htmlPreview with
                {
                    Title = htmlPreview.Title ?? oembed.Title,
                    Author = htmlPreview.Author ?? oembed.AuthorName,
                    SiteName = htmlPreview.SiteName ?? oembed.ProviderName,
                    ImageUrl = htmlPreview.ImageUrl ?? oembed.ThumbnailUrl,
                };
            }
        }

        var merged = providerPreview?.MergeWith(htmlPreview) ?? htmlPreview;
        return Finalize(merged, uri);
    }

    /// <summary>
    /// Fills site name and icon so even a blocked page renders as "domain + icon".
    /// </summary>
    private WebPreview Finalize(WebPreview preview, Uri requestUri)
    {
        var host = (Uri.TryCreate(preview.FinalUrl, UriKind.Absolute, out var final) ? final : requestUri).Host;
        var displayHost = host.StartsWith("www.", StringComparison.OrdinalIgnoreCase) ? host[4..] : host;

        var icon = preview.IconUrl;
        if (icon == null && _options.FallbackIconUrlTemplate != null)
        {
            icon = string.Format(System.Globalization.CultureInfo.InvariantCulture, _options.FallbackIconUrlTemplate, Uri.EscapeDataString(host));
        }

        return preview with
        {
            SiteName = preview.SiteName ?? displayHost,
            IconUrl = icon,
        };
    }

    private sealed record CacheEntry(DateTimeOffset Expires, WebPreview Preview);
}