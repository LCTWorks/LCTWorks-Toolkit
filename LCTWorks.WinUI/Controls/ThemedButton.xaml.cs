using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace LCTWorks.WinUI.Controls;

public sealed partial class ThemedButton : Button
{
    public static readonly DependencyProperty BackgroundDisabledProperty =
        DependencyProperty.Register(
        nameof(BackgroundDisabled),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty BackgroundPointerOverProperty =
            DependencyProperty.Register(
        nameof(BackgroundPointerOver),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty BackgroundPressedProperty =
        DependencyProperty.Register(
        nameof(BackgroundPressed),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty BorderBrushDisabledProperty =
        DependencyProperty.Register(
        nameof(BorderBrushDisabled),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty BorderBrushPointerOverProperty =
            DependencyProperty.Register(
        nameof(BorderBrushPointerOver),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty BorderBrushPressedProperty =
        DependencyProperty.Register(
        nameof(BorderBrushPressed),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty ForegroundDisabledProperty =
        DependencyProperty.Register(
        nameof(ForegroundDisabled),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty ForegroundPointerOverProperty =
        DependencyProperty.Register(
        nameof(ForegroundPointerOver),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty ForegroundPressedProperty =
            DependencyProperty.Register(
        nameof(ForegroundPressed),
        typeof(Brush),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty GlyphFontFamilyProperty =
        DependencyProperty.Register(
        nameof(GlyphFontFamily),
        typeof(FontFamily),
        typeof(ThemedButton),
        new PropertyMetadata(default));

    public static readonly DependencyProperty GlyphFontSizeProperty =
        DependencyProperty.Register(
        nameof(GlyphFontSize),
        typeof(double),
        typeof(ThemedButton),
        new PropertyMetadata(0.0));

    public static readonly DependencyProperty GlyphProperty =
            DependencyProperty.Register(
        nameof(Glyph),
        typeof(string),
        typeof(ThemedButton),
        new PropertyMetadata(string.Empty, OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(
        nameof(Icon),
        typeof(IconElement),
        typeof(ThemedButton),
        new PropertyMetadata(null, OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty SeparationMarginProperty =
        DependencyProperty.Register(
        nameof(SeparationMargin),
        typeof(Thickness),
        typeof(ThemedButton),
        new PropertyMetadata(new Thickness(0), OnLeadingVisualPropertyChanged));

    public static readonly DependencyProperty ShowGlyphProperty =
        DependencyProperty.Register(
        nameof(ShowGlyph),
        typeof(bool),
        typeof(ThemedButton),
        new PropertyMetadata(true, OnLeadingVisualPropertyChanged));

    private static readonly DependencyProperty GlyphVisibilityInternalProperty =
        DependencyProperty.Register(
            nameof(GlyphVisibilityInternal),
            typeof(Visibility),
            typeof(ThemedButton),
            new PropertyMetadata(Visibility.Visible));

    private static readonly DependencyProperty IconVisibilityInternalProperty =
        DependencyProperty.Register(
            nameof(IconVisibilityInternal),
            typeof(Visibility),
            typeof(ThemedButton),
            new PropertyMetadata(Visibility.Collapsed));

    private static readonly DependencyProperty SeparationMarginInternalProperty =
        DependencyProperty.Register(
            nameof(SeparationMarginInternal),
            typeof(Thickness),
            typeof(ThemedButton),
            new PropertyMetadata(default));

    public ThemedButton()
    {
        DefaultStyleKey = typeof(ThemedButton);
    }

    public Brush BackgroundDisabled
    {
        get => (Brush)GetValue(BackgroundDisabledProperty);
        set => SetValue(BackgroundDisabledProperty, value);
    }

    public Brush BackgroundPointerOver
    {
        get => (Brush)GetValue(BackgroundPointerOverProperty);
        set => SetValue(BackgroundPointerOverProperty, value);
    }

    public Brush BackgroundPressed
    {
        get => (Brush)GetValue(BackgroundPressedProperty);
        set => SetValue(BackgroundPressedProperty, value);
    }

    public Brush BorderBrushDisabled
    {
        get => (Brush)GetValue(BorderBrushDisabledProperty);
        set => SetValue(BorderBrushDisabledProperty, value);
    }

    public Brush BorderBrushPointerOver
    {
        get => (Brush)GetValue(BorderBrushPointerOverProperty);
        set => SetValue(BorderBrushPointerOverProperty, value);
    }

    public Brush BorderBrushPressed
    {
        get => (Brush)GetValue(BorderBrushPressedProperty);
        set => SetValue(BorderBrushPressedProperty, value);
    }

    public Brush ForegroundDisabled
    {
        get => (Brush)GetValue(ForegroundDisabledProperty);
        set => SetValue(ForegroundDisabledProperty, value);
    }

    public Brush ForegroundPointerOver
    {
        get => (Brush)GetValue(ForegroundPointerOverProperty);
        set => SetValue(ForegroundPointerOverProperty, value);
    }

    public Brush ForegroundPressed
    {
        get => (Brush)GetValue(ForegroundPressedProperty);
        set => SetValue(ForegroundPressedProperty, value);
    }

    /// <summary>
    /// Gets or sets the glyph character(s) to display before the content.
    /// Ignored when <see cref="Icon"/> is set.
    /// </summary>
    public string Glyph
    {
        get => (string)GetValue(GlyphProperty);
        set => SetValue(GlyphProperty, value);
    }

    /// <summary>
    /// Gets or sets an <see cref="IconElement"/> to display before the content.
    /// When set, it replaces the <see cref="Glyph"/> text block.
    /// </summary>
    public IconElement? Icon
    {
        get => (IconElement?)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
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

    /// <summary>
    /// Gets or sets whether the leading visual (<see cref="Glyph"/> or <see cref="Icon"/>) is shown.
    /// </summary>
    public bool ShowGlyph
    {
        get => (bool)GetValue(ShowGlyphProperty);
        set => SetValue(ShowGlyphProperty, value);
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

    protected override void OnContentChanged(object oldContent, object newContent)
    {
        base.OnContentChanged(oldContent, newContent);
        UpdateLeadingVisual();
    }

    private static void OnLeadingVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        ((ThemedButton)d).UpdateLeadingVisual();
    }

    private void UpdateLeadingVisual()
    {
        bool hasIcon = Icon is not null;
        bool hasGlyph = !hasIcon && !string.IsNullOrEmpty(Glyph);
        bool hasContent = Content is not null && (Content as string)?.Length != 0;

        IconVisibilityInternal = ShowGlyph && hasIcon ? Visibility.Visible : Visibility.Collapsed;
        GlyphVisibilityInternal = ShowGlyph && !hasIcon ? Visibility.Visible : Visibility.Collapsed;

        bool hasLeadingVisual = ShowGlyph && (hasIcon || hasGlyph);
        SeparationMarginInternal = hasLeadingVisual && hasContent ? SeparationMargin : new Thickness(0);
    }
}