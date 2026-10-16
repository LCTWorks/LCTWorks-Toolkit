namespace LCTWorks.Web;

/// <summary>
/// A link preview: everything needed to render a link card.
/// </summary>
public sealed record WebPreview
{
    /// <summary>
    /// The URL that was requested.
    /// </summary>
    public required string Url { get; init; }

    /// <summary>
    /// The canonical/final URL after redirects or provider normalization.
    /// </summary>
    public string? FinalUrl { get; init; }

    public string? Title { get; init; }

    public string? Description { get; init; }

    public string? SiteName { get; init; }

    public string? Author { get; init; }

    /// <summary>
    /// Thumbnail / preview image (absolute URL).
    /// </summary>
    public string? ImageUrl { get; init; }

    /// <summary>
    /// Site icon (absolute URL).
    /// </summary>
    public string? IconUrl { get; init; }

    /// <summary>
    /// Embeddable video URL, if any.
    /// </summary>
    public string? VideoUrl { get; init; }

    /// <summary>
    /// Content flagged as adult/NSFW by the source (e.g. Reddit's over_18).
    /// </summary>
    public bool IsNsfw { get; init; }

    /// <summary>
    /// Which provider produced the main data ("YouTube", "Reddit", "Amazon", "oEmbed", "html").
    /// </summary>
    public string? Provider { get; init; }

    /// <summary>
    /// True when the site answered with a bot challenge/captcha and only fallback data is available.
    /// </summary>
    public bool IsBlocked { get; init; }

    public string? BlockReason { get; init; }

    public bool IsComplete => !string.IsNullOrWhiteSpace(Title) && !string.IsNullOrWhiteSpace(ImageUrl);

    public bool IsEmpty => string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(ImageUrl) && string.IsNullOrWhiteSpace(Description);

    /// <summary>
    /// Fills missing values from <paramref name="fallback"/>; values already set on this instance win.
    /// </summary>
    public WebPreview MergeWith(WebPreview? fallback)
        => fallback == null
            ? this
            : this with
            {
                FinalUrl = FinalUrl ?? fallback.FinalUrl,
                Title = Title ?? fallback.Title,
                Description = Description ?? fallback.Description,
                SiteName = SiteName ?? fallback.SiteName,
                Author = Author ?? fallback.Author,
                ImageUrl = ImageUrl ?? fallback.ImageUrl,
                IconUrl = IconUrl ?? fallback.IconUrl,
                VideoUrl = VideoUrl ?? fallback.VideoUrl,
                IsNsfw = IsNsfw || fallback.IsNsfw,
                IsBlocked = IsEmpty && (IsBlocked || fallback.IsBlocked),
                BlockReason = BlockReason ?? fallback.BlockReason,
            };
}