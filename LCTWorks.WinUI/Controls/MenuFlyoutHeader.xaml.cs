using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls;

public sealed partial class MenuFlyoutHeader : MenuFlyoutSeparator
{
    public static readonly DependencyProperty HeaderProperty =
        DependencyProperty.Register(
        nameof(Header),
        typeof(string),
        typeof(MenuFlyoutHeader),
        new PropertyMetadata(string.Empty));

    public MenuFlyoutHeader()
    {
        DefaultStyleKey = typeof(MenuFlyoutHeader);
    }

    public string Header
    {
        get => (string)GetValue(HeaderProperty);
        set => SetValue(HeaderProperty, value);
    }
}