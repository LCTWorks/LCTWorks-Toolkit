using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LCTWorks.WinUI.Controls;

public sealed partial class Chip : Control
{
    public static readonly DependencyProperty GlyphFontFamilyProperty =
        DependencyProperty.Register(
        nameof(GlyphFontFamily),
        typeof(FontFamily),
        typeof(Chip),
        new PropertyMetadata(default));

    public static readonly DependencyProperty GlyphFontSizeProperty =
        DependencyProperty.Register(
        nameof(GlyphFontSize),
        typeof(double),
        typeof(Chip),
        new PropertyMetadata(0.0));

    public static readonly DependencyProperty GlyphProperty =
        DependencyProperty.Register(
        nameof(Glyph),
        typeof(string),
        typeof(Chip),
        new PropertyMetadata(string.Empty, OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
        nameof(Icon),
        typeof(IconElement),
        typeof(Chip),
        new PropertyMetadata(null, OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty SeparationMarginProperty =
        DependencyProperty.Register(
        nameof(SeparationMargin),
        typeof(Thickness),
        typeof(Chip),
        new PropertyMetadata(new Thickness(0), OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty ShowGlyphProperty =
        DependencyProperty.Register(
        nameof(ShowGlyph),
        typeof(bool),
        typeof(Chip),
        new PropertyMetadata(true, OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(Chip),
        new PropertyMetadata(string.Empty, OnLeadingVisualPropertyChanged));

    private static readonly DependencyProperty GlyphVisibilityInternalProperty =
        DependencyProperty.Register(
            nameof(GlyphVisibilityInternal),
            typeof(Visibility),
            typeof(Chip),
            new PropertyMetadata(Visibility.Visible));

    private static readonly DependencyProperty IconVisibilityInternalProperty =
        DependencyProperty.Register(
            nameof(IconVisibilityInternal),
            typeof(Visibility),
            typeof(Chip),
            new PropertyMetadata(Visibility.Collapsed));

    private static readonly DependencyProperty SeparationMarginInternalProperty =
        DependencyProperty.Register(
            nameof(SeparationMarginInternal),
            typeof(Thickness),
            typeof(Chip),
            new PropertyMetadata(default));

    public Chip()
    {
        DefaultStyleKey = typeof(Chip);
    }

    /// <summary>
    /// Gets or sets the glyph character(s) to display before the text.
    /// Ignored when <see cref="Icon"/> is set.
    /// </summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    public FontFamily GlyphFontFamily
    {
        get => (FontFamily)GetValue(GlyphFontFamilyProperty);
        set => SetValue(GlyphFontFamilyProperty, value);
    }

    public double GlyphFontSize
    {
        get => (double)GetValue(GlyphFontSizeProperty);
        set => SetValue(GlyphFontSizeProperty, value);
    }

    /// <summary>
    /// Gets or sets an <see cref="IconElement"/> to display before the text.
    /// When set, it replaces the <see cref="Glyph"/> text block.
    /// </summary>
    public IconElement? Icon
    {
        get => (IconElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public Thickness SeparationMargin
    {
        get => (Thickness)GetValue(SeparationMarginProperty);
        set => SetValue(SeparationMarginProperty, value);
    }

    /// <summary>
    /// Gets or sets whether the leading visual (<see cref="Glyph"/> or <see cref="Icon"/>) is shown.
    /// </summary>
    public bool ShowGlyph
    {
        get => (bool)GetValue(ShowGlyphProperty);
        set => SetValue(ShowGlyphProperty, value);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    private Visibility GlyphVisibilityInternal
    {
        get => (Visibility)GetValue(GlyphVisibilityInternalProperty);
        set => SetValue(GlyphVisibilityInternalProperty, value);
    }

    private Visibility IconVisibilityInternal
    {
        get => (Visibility)GetValue(IconVisibilityInternalProperty);
        set => SetValue(IconVisibilityInternalProperty, value);
    }

    private Thickness SeparationMarginInternal
    {
        get => (Thickness)GetValue(SeparationMarginInternalProperty);
        set => SetValue(SeparationMarginInternalProperty, value);
    }

    private static void OnLeadingVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Chip)d).UpdateLeadingVisual();
    }

    private void UpdateLeadingVisual()
    {
        bool hasIcon = Icon is not null;
        bool hasGlyph = !hasIcon && !string.IsNullOrEmpty(Glyph);
        bool hasText = !string.IsNullOrEmpty(Text);

        IconVisibilityInternal = ShowGlyph && hasIcon ? Visibility.Visible : Visibility.Collapsed;
        GlyphVisibilityInternal = ShowGlyph && !hasIcon ? Visibility.Visible : Visibility.Collapsed;

        bool hasLeadingVisual = ShowGlyph && (hasIcon || hasGlyph);
        SeparationMarginInternal = hasLeadingVisual && hasText ? SeparationMargin : new Thickness(0);
    }
}
