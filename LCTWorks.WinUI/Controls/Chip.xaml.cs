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
        new PropertyMetadata(string.Empty, OnSeparationPropertyChanged));

    public static readonly DependencyProperty SeparationMarginProperty =
        DependencyProperty.Register(
        nameof(SeparationMargin),
        typeof(Thickness),
        typeof(Chip),
        new PropertyMetadata(new Thickness(0), OnSeparationPropertyChanged));

    public static readonly DependencyProperty ShowGlyphProperty =
        DependencyProperty.Register(
        nameof(ShowGlyph),
        typeof(bool),
        typeof(Chip),
        new PropertyMetadata(true, OnSeparationPropertyChanged));

    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(Chip),
        new PropertyMetadata(string.Empty, OnSeparationPropertyChanged));

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

    public Thickness SeparationMargin
    {
        get => (Thickness)GetValue(SeparationMarginProperty);
        set => SetValue(SeparationMarginProperty, value);
    }

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

    private Thickness SeparationMarginInternal
    {
        get => (Thickness)GetValue(SeparationMarginInternalProperty);
        set => SetValue(SeparationMarginInternalProperty, value);
    }

    private static void OnSeparationPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((Chip)d).UpdateSeparation();
    }

    private void UpdateSeparation()
    {
        bool hasGlyph = ShowGlyph && !string.IsNullOrEmpty(Glyph);
        bool hasText = !string.IsNullOrEmpty(Text);
        SeparationMarginInternal = (hasGlyph && hasText) ? SeparationMargin : new Thickness(0);
    }
}
