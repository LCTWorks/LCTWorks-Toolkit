using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls;

public sealed partial class ChipControl : Control
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(ChipControl),
        new PropertyMetadata(string.Empty));

    public ChipControl()
    {
        DefaultStyleKey = typeof(ChipControl);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}