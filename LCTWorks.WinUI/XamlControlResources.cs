using System;
using Microsoft.UI.Xaml;

namespace LCTWorks.WinUI;

public partial class XamlControlsResources : ResourceDictionary
{
    public XamlControlsResources()
    {
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Themes/Constants.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Themes/FontFamilies.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Themes/TextBlocks.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Controls/Chip.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Controls/ThemedButton.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Controls/MenuFlyoutHeader.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Controls/NavigationViewItemButton.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Controls/AdaptiveImage.xaml", UriKind.Absolute) });
        MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("ms-appx:///LCTWorks.WinUI/Controls/AdaptiveView.xaml", UriKind.Absolute) });
    }
}