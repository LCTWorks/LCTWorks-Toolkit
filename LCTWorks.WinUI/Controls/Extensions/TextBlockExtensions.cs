using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls.Extensions;

public static class TextBlockExtensions
{
    public static readonly DependencyProperty HideIfTextEmptyProperty =
        DependencyProperty.RegisterAttached(
            "HideIfTextEmpty",
            typeof(bool),
            typeof(TextBlockExtensions),
            new PropertyMetadata(false, OnHideIfEmptyChanged));

    public static bool GetHideIfTextEmpty(TextBlock textBlock)
    {
        return (bool)textBlock.GetValue(HideIfTextEmptyProperty);
    }

    public static void SetHideIfTextEmpty(TextBlock textBlock, bool value)
    {
        textBlock.SetValue(HideIfTextEmptyProperty, value);
    }

    private static void OnHideIfEmptyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is TextBlock textBlock)
        {
            textBlock.Loaded -= TextBlock_Loaded;
            textBlock.Loaded += TextBlock_Loaded;
            UpdateVisibility(textBlock);
            textBlock.RegisterPropertyChangedCallback(TextBlock.TextProperty, (sender, dp) =>
            {
                UpdateVisibility(textBlock);
            });
        }
    }

    private static void TextBlock_Loaded(object sender, RoutedEventArgs e)
    {
        if (sender is TextBlock textBlock)
        {
            UpdateVisibility(textBlock);
        }
    }

    private static void UpdateVisibility(TextBlock textBlock)
    {
        bool hideIfEmpty = GetHideIfTextEmpty(textBlock);
        if (hideIfEmpty)
        {
            textBlock.Visibility = string.IsNullOrEmpty(textBlock.Text)
                ? Visibility.Collapsed
                : Visibility.Visible;
        }
    }
}