using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls;

public sealed partial class NavigationViewItemButton : Button
{
    public NavigationViewItemButton()
    {
        DefaultStyleKey = typeof(NavigationViewItemButton);
    }

    public IconElement Icon
    {
        get => (IconElement)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(IconElement), typeof(NavigationViewItemButton),
            new PropertyMetadata(default));
}