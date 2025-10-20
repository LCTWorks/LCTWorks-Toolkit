using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls;

public sealed partial class Chip : Control
{
    public static readonly DependencyProperty TextProperty =
        DependencyProperty.Register(
        nameof(Text),
        typeof(string),
        typeof(Chip),
        new PropertyMetadata(string.Empty));

    public Chip()
    {
        DefaultStyleKey = typeof(Chip);
    }

    public string Text
    {
        get => (string)GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }
}