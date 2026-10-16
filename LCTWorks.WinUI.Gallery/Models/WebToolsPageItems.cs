using System.Globalization;
using LCTWorks.Web;

namespace LCTWorks.Workshop.Models;

// XAML-facing view items for WebToolsPage.
// The XAML compiler generates metadata (activators/setters) for every type used by x:Bind, which fails
// for records with init-only or required members (HtmlImage, WebPreview). These wrappers expose
// get-only properties and have no parameterless constructor, so nothing needs to be activated or set.

/// <summary>
/// An image found by <c>WebCrawler.GetAllImagesAsync</c>.
/// </summary>
public sealed class ImageItem(HtmlImage image)
{
    public string SizeText { get; } = image.Width is > 0 || image.Height is > 0
            ? $"{image.Width?.ToString(CultureInfo.InvariantCulture) ?? "?"} × {image.Height?.ToString(CultureInfo.InvariantCulture) ?? "?"}"
            : "size unknown";

    public string Src { get; } = image.Src ?? string.Empty;
}

/// <summary>
/// A name/value row shown in the raw output lists.
/// </summary>
public sealed class PropertyRow(string name, string value)
{
    public string Name { get; } = name;

    public string Value { get; } = value;
}

/// <summary>
/// A sample URL shown as an icon in the options pane.
/// </summary>
public sealed class SampleLink(string name, string url, string? icon = null)
{
    public const string DefaultIcon = "ms-appx:///Assets/Icons/Brands/edge.svg";

    public string Icon { get; } = icon ?? DefaultIcon;

    public string Name { get; } = name;

    public string ToolTip { get; } = $"{name}\n{url}";

    public string Url { get; } = url;
}

/// <summary>
/// The link card data, built from a <see cref="WebPreview"/>.
/// </summary>
public sealed class PreviewCard(WebPreview preview)
{
    public string Author { get; } = preview.Author ?? string.Empty;

    public string BlockReason { get; } = preview.BlockReason ?? string.Empty;

    public string Description { get; } = preview.Description ?? string.Empty;

    public string? IconUrl { get; } = preview.IconUrl;

    public string? ImageUrl { get; } = preview.ImageUrl;

    public bool IsBlocked { get; } = preview.IsBlocked;

    public bool IsNsfw { get; } = preview.IsNsfw;

    public string LinkText { get; } = !string.IsNullOrWhiteSpace(preview.FinalUrl) ? preview.FinalUrl : preview.Url;

    public string SiteName { get; } = preview.SiteName ?? string.Empty;

    public string Title { get; } = preview.Title ?? string.Empty;
}