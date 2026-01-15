namespace LCTWorks.Web;

public record class HtmlMetaTags
{
    // Basic Meta Tags
    public string? Title { get; init; }
    public string? Description { get; init; }
    public string? Keywords { get; init; }
    public string? Author { get; init; }
    public string? Generator { get; init; }
    public string? Charset { get; init; }
    public string? Viewport { get; init; }
    public string? Robots { get; init; }
    public string? CanonicalUrl { get; init; }
    public string? Language { get; init; }
    public string? ThemeColor { get; init; }
    public string? ColorScheme { get; init; }
    public string? ApplicationName { get; init; }

    // Open Graph (og:) Tags
    public string? OgTitle { get; init; }
    public string? OgDescription { get; init; }
    public string? OgType { get; init; }
    public string? OgUrl { get; init; }
    public string? OgImage { get; init; }
    public string? OgImageAlt { get; init; }
    public string? OgImageWidth { get; init; }
    public string? OgImageHeight { get; init; }
    public string? OgSiteName { get; init; }
    public string? OgLocale { get; init; }

    // Twitter Card Tags
    public string? TwitterCard { get; init; }
    public string? TwitterSite { get; init; }
    public string? TwitterCreator { get; init; }
    public string? TwitterTitle { get; init; }
    public string? TwitterDescription { get; init; }
    public string? TwitterImage { get; init; }
    public string? TwitterImageAlt { get; init; }

    // Article Meta (Open Graph)
    public string? ArticleAuthor { get; init; }
    public string? ArticlePublishedTime { get; init; }
    public string? ArticleModifiedTime { get; init; }
    public string? ArticleSection { get; init; }
    public string? ArticleTags { get; init; }

    // Apple/iOS Specific
    public string? AppleMobileWebAppCapable { get; init; }
    public string? AppleMobileWebAppTitle { get; init; }
    public string? AppleMobileWebAppStatusBarStyle { get; init; }
    public string? AppleTouchIcon { get; init; }

    // Microsoft Specific
    public string? MsApplicationTileColor { get; init; }
    public string? MsApplicationTileImage { get; init; }

    // Other Common Tags
    public string? Favicon { get; init; }
    public string? Copyright { get; init; }
    public string? Rating { get; init; }
    public string? Referrer { get; init; }
    public string? FormatDetection { get; init; }

    //Convenience Methods:
    public string? GetTitle()
        => Title ?? OgTitle ?? TwitterTitle;

    public string? GetDescription()
        => Description ?? OgDescription ?? TwitterDescription;

    public string? GetImage()
        => OgImage ?? TwitterImage;

    public string? GetImageAlt()
        => OgImageAlt ?? TwitterImageAlt;

    public string? GetAuthor()
        => Author ?? ArticleAuthor;

    public string? GetCanonicalUrl()
        => CanonicalUrl ?? OgUrl;
}