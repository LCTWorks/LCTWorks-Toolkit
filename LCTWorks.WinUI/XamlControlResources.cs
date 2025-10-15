using System;
using Microsoft.UI.Xaml;

namespace LCTWorks.WinUI;

public partial class XamlControlsResources : ResourceDictionary
{
    public XamlControlsResources()
    {
        Source = new Uri("ms-appx:///LCTWorks.WinUI/Themes/Generic.xaml", UriKind.Absolute);
    }
}