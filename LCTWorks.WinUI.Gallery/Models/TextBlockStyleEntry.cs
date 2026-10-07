using Microsoft.UI.Xaml;

namespace LCTWorks.Workshop.Models;

public class TextBlockStyleEntry
{
    private const string DemoText = "The quick brown fox jumps over the lazy dog. Pack my box with five dozen liquor jugs.";

    /// <summary>
    /// Resource key of the style this one is based on, or <c>null</c> when it has no base.
    /// </summary>
    public string? BasedOn { get; set; }

    public string BasedOnDisplay => string.IsNullOrEmpty(BasedOn) ? "—" : BasedOn;

    public bool IsValid => Style != null;

    public string? ResourceName { get; set; }

    /// <summary>
    /// Text rendered with <see cref="Style"/> in the sample column.
    /// </summary>
    public string SampleText { get; set; } = DemoText;

    public Style? Style { get; set; }
}