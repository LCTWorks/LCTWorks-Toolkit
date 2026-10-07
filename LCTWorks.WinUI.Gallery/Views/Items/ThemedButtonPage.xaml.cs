using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;
using LCTWorks.WinUI.Xaml.Extensions;
using LCTWorks.Workshop.Internal;
using LCTWorks.Workshop.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

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

    public string Header { get; } = "ThemedButton_Title".GetTextLocalized();

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

    private List<ThemedButtonStyleEntry> Styles { get; set; } = [];

    private static ThemedButtonStyleEntry GetEntryFromResources(string styleKey)
    {
        var style = Application.Current.Resources[styleKey] as Style;
        var entry = new ThemedButtonStyleEntry { ResourceName = styleKey, Style = style };
        return entry;
    }

    private void CopyTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is ThemedButtonStyleEntry entry && entry.ValidStyle)
        {
            try
            {
                var package = new DataPackage
                {
                    RequestedOperation = DataPackageOperation.Copy
                };
                package.SetText(entry.ResourceName);
                Clipboard.SetContent(package);

                InfoBar.Message = "Resource name copied to clipboard";
                InfoBar.IsOpen = true;
            }
            catch
            {
            }
        }
    }

    private void InitProperties()
    {
        ButtonContent = "Content";
        GlyphText = "\\uE7C5";
        GlyphFontSize = 20;
        ShowGlyph = true;
        SeparationHorizontal = 6;
        SeparationVertical = 0;
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
}