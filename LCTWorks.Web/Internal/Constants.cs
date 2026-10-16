namespace LCTWorks.Web.Internal;

internal static class Constants
{
    /// <summary>
    /// Overall timeout for a single HTTP request.
    /// </summary>
    internal static readonly TimeSpan HttpClientTimeout = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long a pooled connection is reused before being recycled (DNS refresh).
    /// </summary>
    internal static readonly TimeSpan PooledConnectionLifetime = TimeSpan.FromMinutes(2);

    /// <summary>
    /// Chromium major version advertised in the User-Agent and client hints. Keep both in sync.
    /// </summary>
    internal const string ChromeMajorVersion = "144";

    internal const string BrowserUserAgent =
        $"Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/{ChromeMajorVersion}.0.0.0 Safari/537.36 Edg/{ChromeMajorVersion}.0.0.0";

    internal const string BrowserClientHints =
        $"\"Chromium\";v=\"{ChromeMajorVersion}\", \"Microsoft Edge\";v=\"{ChromeMajorVersion}\", \"Not?A_Brand\";v=\"99\"";

    /// <summary>
    /// Honest link-preview User-Agent, used as a fallback when the browser profile is blocked.
    /// Many sites serve a lightweight, OG-tags-only page to clients that identify as preview bots.
    /// </summary>
    internal const string PreviewBotUserAgent =
        "Mozilla/5.0 (compatible; LCTWorksPreviewBot/1.0; +https://github.com/LCTWorks/LCTWorks-Toolkit)";

    /// <summary>
    /// Maximum number of bytes read from an HTML document. Metadata lives in &lt;head&gt;, but some
    /// fallbacks (e.g. Amazon's product image) sit in the body, so allow a generous but bounded size.
    /// </summary>
    internal const int MaxHtmlBytes = 2 * 1024 * 1024;
}
