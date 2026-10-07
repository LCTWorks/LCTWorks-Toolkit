using Microsoft.UI.Xaml.Media;

namespace LCTWorks.Workshop.Models;

public class FontFamilyEntry
{
    public string? ResourceName { get; set; }

    public FontFamily? FontFamily { get; set; }

    /// <summary>
    /// Text rendered with <see cref="FontFamily"/> in the sample column.
    /// </summary>
    public string SampleText { get; set; } = "The quick brown fox jumps over the lazy dog";

    /// <summary>
    /// Where the resource points: the font file and face for a direct definition,
    /// or the aliased key for a <c>StaticResource</c>.
    /// </summary>
    public string? Notes { get; set; }

    public bool IsValid => FontFamily != null;

    /// <summary>
    /// Font used by the sample column; falls back to the default font when the resource is missing,
    /// since assigning a null <see cref="FontFamily"/> to a TextBlock is not allowed.
    /// </summary>
    public FontFamily SampleFontFamily => FontFamily ?? FontFamily.XamlAutoFontFamily;
}
