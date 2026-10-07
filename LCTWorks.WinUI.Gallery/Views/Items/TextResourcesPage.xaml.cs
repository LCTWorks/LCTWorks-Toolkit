using LCTWorks.WinUI.Controls;
using LCTWorks.WinUI.Extensions;
using LCTWorks.Workshop.Models;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;
using System.Collections.Generic;
using System.Linq;
using Windows.ApplicationModel.DataTransfer;

namespace LCTWorks.Workshop.Views.Items;

public sealed partial class TextResourcesPage : ObservablePage
{
    private const string FluentSample = "       ";
    private const string FontAwesomeSample = "     ";
    private const string MaterialSample = "     ";

    // Keys from LCTWorks.WinUI/Themes/FontFamilies.xaml.
    // Value is the aliased key when the entry is a <StaticResource>, otherwise null.
    private static readonly (string Key, string? AliasOf, string? Sample)[] FontFamilyKeys =
    [
        ("PoppinsBoldFontFamily", null, null),
        ("PoppinsRegularFontFamily", null, null),
        ("PoppinsLightFontFamily", null, null),
        ("SmoothRegularFontFamily", "PoppinsRegularFontFamily", null),
        ("SmoothBoldFontFamily", "PoppinsBoldFontFamily", null),
        ("SmoothLightFontFamily", "PoppinsLightFontFamily", null),
        ("FluentIconsFontFamily", null, FluentSample),
        ("MaterialSymbolsFontFamily", null, MaterialSample),
        ("FontAwesomeBrandsFontFamily", null, FontAwesomeSample),
    ];

    // Keys from LCTWorks.WinUI/Themes/TextBlocks.xaml.
    private static readonly (string Key, string? Sample)[] TextBlockStyleKeys =
    [
        // Base styles
        ("SmoothRegularTextBlockStyle", null),
        ("SmoothLightTextBlockStyle", null),
        ("SmoothBoldTextBlockStyle", null),
        // General text styles
        ("SmoothHeaderTextBlockStyle", null),
        ("SmoothTitleTextBlockStyle", null),
        ("SmoothSubtitleTextBlockStyle", null),
        ("SmoothBodyTextBlockStyle", null),
        ("SmoothCaptionTextBlockStyle", null),
        // Item text styles
        ("SmoothItemTitleTextBlockStyle", null),
        ("SmoothItemBodyTextBlockStyle", null),
        ("SmoothItemCaptionTextBlockStyle", null),
                // Icon styles
        ("FluentIconsTextBlockStyle", FluentSample),
        ("FluentIconsItemTextBlockStyle", FluentSample),
        ("MaterialSymbolsTextBlockStyle", MaterialSample),
        ("FontAwesomeBrandsTextBlockStyle", FontAwesomeSample),
    ];

    public TextResourcesPage()
    {
        InitializeComponent();

        InitProperties();
    }

    public string Description { get; } = "TextResources_Description".GetTextLocalized();

    public string Header { get; } = "TextResources_Title".GetTextLocalized();

    private List<FontFamilyEntry> FontFamilies { get; } = [];

    private List<TextBlockStyleEntry> TextBlockStyles { get; } = [];

    private static string DescribeFontSource(FontFamily fontFamily)
    {
        // e.g. ms-appx:///LCTWorks.WinUI/Assets/Fonts/Poppins-Regular.ttf#Poppins
        var source = fontFamily.Source ?? string.Empty;
        var fileAndFace = source[(source.LastIndexOf('/') + 1)..];
        var hashIndex = fileAndFace.IndexOf('#');
        if (hashIndex < 0)
        {
            return fileAndFace;
        }

        var file = fileAndFace[..hashIndex];
        var face = fileAndFace[(hashIndex + 1)..];
        return $"{file}  ·  \"{face}\"";
    }

    private void CopyTapped(object sender, Microsoft.UI.Xaml.Input.TappedRoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is string resourceName && !string.IsNullOrEmpty(resourceName))
        {
            try
            {
                var package = new DataPackage
                {
                    RequestedOperation = DataPackageOperation.Copy
                };
                package.SetText(resourceName);
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
        var resources = Application.Current.Resources;

        foreach (var (key, aliasOf, sample) in FontFamilyKeys)
        {
            var fontFamily = resources.TryGetValue(key, out var value) ? value as FontFamily : null;
            var entry = new FontFamilyEntry
            {
                ResourceName = key,
                FontFamily = fontFamily,
                Notes = fontFamily is null
                    ? "Resource not found"
                    : aliasOf is not null
                        ? $"StaticResource → {aliasOf}"
                        : DescribeFontSource(fontFamily),
            };
            if (sample is not null)
            {
                entry.SampleText = sample;
            }
            FontFamilies.Add(entry);
        }

        // Load all styles first so BasedOn can be resolved by reference.
        var styles = new Dictionary<string, Style>();
        foreach (var (key, _) in TextBlockStyleKeys)
        {
            if (resources.TryGetValue(key, out var value) && value is Style style)
            {
                styles[key] = style;
            }
        }
        var keyByStyle = styles.ToDictionary(kv => kv.Value, kv => kv.Key, ReferenceEqualityComparer.Instance);

        foreach (var (key, sample) in TextBlockStyleKeys)
        {
            styles.TryGetValue(key, out var style);
            string? basedOn = null;
            if (style?.BasedOn is Style baseStyle)
            {
                basedOn = keyByStyle.TryGetValue(baseStyle, out var baseKey) ? baseKey : "-";
            }

            var entry = new TextBlockStyleEntry
            {
                ResourceName = key,
                Style = style,
                BasedOn = basedOn,
            };
            if (sample is not null)
            {
                entry.SampleText = sample;
            }
            TextBlockStyles.Add(entry);
        }
    }
}