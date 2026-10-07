using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.Internal;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class ChipPage : ObservablePage
{
    private static readonly Dictionary<string, string> GlyphFontFamilies = new()
    {
        { "Segoe Fluent Icons", "FluentIconsFontFamily"},
        { "Material Symbols", "MaterialSymbolsFontFamily"},
        { "FontAwesome Brands", "FontAwesomeBrandsFontFamily"},
    };

    public ChipPage()
    {
        InitializeComponent();

        InitProperties();
    }

    public string? ChipText
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Description { get; } = "Chip_Description".GetTextLocalized();

    public string? Glyph
    {
        get;
        private set => SetProperty(ref field, value);
    }

    public string? GlyphText
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                Glyph = GlyphParser.Parse(value);
            }
        }
    }

    public string? GlyphFontFamily
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                SetValuesFromResources();
            }
        }
    }

    public double GlyphFontSize
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Header { get; } = "Chip_Title".GetTextLocalized();

    public double SeparationHorizontal
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(SeparationMargin));
            }
        }
    }

    public Thickness SeparationMargin => new(SeparationHorizontal, SeparationVertical, 0, 0);

    public double SeparationVertical
    {
        get;
        set
        {
            if (SetProperty(ref field, value))
            {
                OnPropertyChanged(nameof(SeparationMargin));
            }
        }
    }

    public bool ShowGlyph
    {
        get;
        set => SetProperty(ref field, value);
    }

    private void InitProperties()
    {
        ChipText = "Text";
        GlyphText = "\\uE705";
        GlyphFontSize = 18;
        ShowGlyph = true;
        SeparationHorizontal = 6;
        SeparationVertical = 0;
        GlyphFontFamilySelector.ItemsSource = GlyphFontFamilies.Keys.ToList();
        GlyphFontFamily = GlyphFontFamilies.First().Key;
    }

    private void SetValuesFromResources()
    {
        FontFamilyWarningTextBlock.Visibility = Visibility.Collapsed;
        if (!string.IsNullOrWhiteSpace(GlyphFontFamily) && GlyphFontFamilies.TryGetValue(GlyphFontFamily, out string? key))
        {
            if (Application.Current.Resources.ContainsKey(key))
            {
                var resource = Application.Current.Resources[key] as FontFamily;
                if (resource != null)
                {
                    SampleChip.GlyphFontFamily = resource;
                    return;
                }
            }
        }
        FontFamilyWarningTextBlock.Visibility = Visibility.Visible;
    }
}
