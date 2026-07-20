using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Xaml.Extensions;
using LCTWorks.Workshop.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class ThemedButtonPage : ObservablePage
{
    private static readonly Dictionary<string, string> GlyphFontFamilies = new()
    {
        { "Segoe Fluent Icons", "FluentIconsFontFamily"},
        { "Material Symbols", "MaterialSymbolsFontFamily"},
        { "FontAwesome Brands", "FontAwesomeBrandsFontFamily"},
    };

    public ThemedButtonPage()
    {
        InitializeComponent();

        InitProperties();
    }

    public string? ButtonContent
    {
        get;
        set => SetProperty(ref field, value);
    }

    public string Description { get; } = "ThemedButton_Description".GetTextLocalized();

    public string? Glyph
    {
        get;
        set => SetProperty(ref field, value);
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

    public string Header { get; } = "ThemedButton_Title".GetTextLocalized();

    public bool ShowGlyph
    {
        get;
        set => SetProperty(ref field, value);
    }

    private List<ThemedButtonStyleEntry> Styles { get; set; } = [];

    private static ThemedButtonStyleEntry GetEntryFromResources(string styleKey)
    {
        var style = Application.Current.Resources[styleKey] as Style;
        var entry = new ThemedButtonStyleEntry { ResourceName = styleKey, Style = style };
        return entry;
    }

    private void InitProperties()
    {
        ButtonContent = "Content";
        Glyph = "\uE7C5";
        GlyphFontSize = 20;
        ShowGlyph = true;
        GlyphFontFamilySelector.ItemsSource = GlyphFontFamilies.Keys.ToList();
        GlyphFontFamily = GlyphFontFamilies.First().Key;

        Styles.Add(new ThemedButtonStyleEntry { ResourceName = "Default style", Style = null });
        Styles.Add(GetEntryFromResources("AccentThemedButtonStyle"));
        Styles.Add(GetEntryFromResources("SuccessThemedButtonStyle"));
        Styles.Add(GetEntryFromResources("WarningThemedButtonStyle"));
        Styles.Add(GetEntryFromResources("DangerThemedButtonStyle"));
        Styles.Add(GetEntryFromResources("DarkGrayThemedButtonStyle"));
        Styles.Add(GetEntryFromResources("WhiteThemedButtonStyle"));
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
                    SampleButton.GlyphFontFamily = resource;
                    return;
                }
            }
        }
        FontFamilyWarningTextBlock.Visibility = Visibility.Visible;
    }

    private void StyleNameTextBlockGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock tb && TextBlockExtensions.GetCopyOnFocus(tb))
        {
            InfoBar.Message = "Resource name copied to clipboard";
            InfoBar.IsOpen = true;
        }
    }
}