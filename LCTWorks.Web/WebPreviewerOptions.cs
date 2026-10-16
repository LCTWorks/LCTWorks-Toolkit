using LCTWorks.Web.Internal.Previews;

namespace LCTWorks.Web;

public sealed class WebPreviewerOptions
{
    /// <summary>
    /// Optional last-resort fetcher for blocked pages, e.g. a headless browser (Playwright/PuppeteerSharp).
    /// Return the rendered HTML, or null.
    /// </summary>
    public Func<Uri, CancellationToken, Task<string?>>? BlockedPageFetcher { get; set; }

    public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(30);

    /// <summary>
    /// HttpClient used for all requests; defaults to <see cref="HttpTools.Client"/>.
    /// </summary>
    public HttpClient? Client { get; set; }

    /// <summary>
    /// Shorter cache for blocked/empty results, so they're retried sooner without hammering the site.
    /// </summary>
    public TimeSpan FailedCacheDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Icon used when the page declares none or couldn't be loaded. {0} is the host.
    /// Set to null to disable (note: the default sends the domain to Google).
    /// </summary>
    public string? FallbackIconUrlTemplate { get; set; } = "https://www.google.com/s2/favicons?domain={0}&sz=128";

    public int MaxCacheEntries { get; set; } = 500;

    /// <summary>
    /// Site-specific providers, tried in order before the generic crawler.
    /// </summary>
    public IList<IPreviewProvider> Providers { get; } =
    [
        new YouTubePreviewProvider(),
        new RedditPreviewProvider(),
        new AmazonPreviewProvider(),
        new OEmbedPreviewProvider(),
    ];

    /// <summary>
    /// Retry blocked pages once identifying as a link-preview bot.
    /// </summary>
    public bool RetryAsPreviewBot { get; set; } = true;

    /// <summary>
    /// Follow the page's &lt;link rel="alternate" type="application/json+oembed"&gt; when title or image is missing.
    /// </summary>
    public bool UseDiscoveredOEmbed { get; set; } = true;
}