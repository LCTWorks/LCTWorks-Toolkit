namespace LCTWorks.Web;

/// <summary>
/// A site-specific preview source (official APIs, oEmbed, known URL patterns).
/// Providers run before the generic HTML crawler.
/// </summary>
public interface IPreviewProvider
{
    /// <summary>
    /// When true and <see cref="GetPreviewAsync"/> returns a non-null result, the generic HTML crawl is skipped
    /// (the provider already crawled, or crawling the site generically is known to be pointless).
    /// When false, the generic crawl runs unless the provider's result is already complete (title + image),
    /// and fills any gaps.
    /// </summary>
    bool IsAuthoritative { get; }

    string Name { get; }

    bool CanHandle(Uri uri);

    /// <summary>
    /// Returns a (possibly partial) preview, or null if this provider couldn't produce anything.
    /// </summary>
    Task<WebPreview?> GetPreviewAsync(Uri uri, HttpClient client, CancellationToken cancellationToken);
}