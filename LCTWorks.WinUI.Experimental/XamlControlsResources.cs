using Microsoft.UI.Xaml;
using System;

namespace LCTWorks.WinUI.Experimental;

public partial class XamlControlsResources : ResourceDictionary
{
    public XamlControlsResources()
    {
        // ms-appx:/// URIs for library resources must be assembly-qualified.
        // Without GenerateLibraryLayout, Themes/Generic.xaml is compiled to XBF and embedded
        // in the DLL — the WinUI runtime loads it via the assembly name prefix.
        Source = new Uri("ms-appx:///LCTWorks.WinUI.Experimental/Themes/Generic.xaml", UriKind.Absolute);
    }
}