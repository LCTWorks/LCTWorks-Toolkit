using XamlVisibility = Microsoft.UI.Xaml.Visibility;

namespace LCTWorks.WinUI.Xaml.Adapters;

public static class Visibility
{
    public static XamlVisibility FromBool(bool value)
        => value ? XamlVisibility.Visible : XamlVisibility.Collapsed;

    public static XamlVisibility FromBoolInverted(bool value)
    => value ? XamlVisibility.Collapsed : XamlVisibility.Visible;
}