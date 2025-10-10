using Microsoft.UI.Xaml;
using System;

namespace LCTWorks.WinUI
{
    public partial class LCTWorksXamlControlsResources : ResourceDictionary
    {
        public LCTWorksXamlControlsResources()
        {
            Source = new Uri("ms-appx:///LCTWorks.WinUI/Themes/Generic.xaml", UriKind.Absolute);
        }
    }
}