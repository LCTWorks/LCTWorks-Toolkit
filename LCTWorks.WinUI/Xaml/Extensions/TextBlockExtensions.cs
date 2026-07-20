using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System;
using Windows.ApplicationModel.DataTransfer;

namespace LCTWorks.WinUI.Xaml.Extensions;

public static class TextBlockExtensions
{
    public static readonly DependencyProperty CopyOnFocusProperty =
       DependencyProperty.RegisterAttached(
           "CopyOnFocus",
           typeof(bool),
           typeof(TextBlockExtensions),
           new PropertyMetadata(false, OnCopyOnFocusChanged));

    public static readonly DependencyProperty HideIfTextEmptyProperty =
           DependencyProperty.RegisterAttached(
           "HideIfTextEmpty",
           typeof(bool),
           typeof(TextBlockExtensions),
           new PropertyMetadata(false, OnHideIfEmptyChanged));

    public static bool GetCopyOnFocus(TextBlock textBlock) =>
        (bool)textBlock.GetValue(CopyOnFocusProperty);

    public static bool GetHideIfTextEmpty(TextBlock textBlock)
    {
        return (bool)textBlock.GetValue(HideIfTextEmptyProperty);
    }

    public static void SetCopyOnFocus(TextBlock textBlock, bool value) =>
        textBlock.SetValue(CopyOnFocusProperty, value);

    public static void SetHideIfTextEmpty(TextBlock textBlock, bool value)
    {
        textBlock.SetValue(HideIfTextEmptyProperty, value);
    }

    private static void OnCopyOnFocusChanged(
        DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not TextBlock textBlock)
            return;

        // Unsubscribe first so toggling the value can't double-wire the handler.
        textBlock.GotFocus -= OnTextBlockGotFocus;

        if (e.NewValue is true)
            textBlock.GotFocus += OnTextBlockGotFocus;
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

    private static void OnTextBlockGotFocus(object sender, RoutedEventArgs e)
    {
        if (sender is not TextBlock textBlock || string.IsNullOrEmpty(textBlock.Text))
            return;

        try
        {
            var package = new DataPackage
            {
                RequestedOperation = DataPackageOperation.Copy
            };
            package.SetText(textBlock.Text);
            Clipboard.SetContent(package);
        }
        catch (Exception)
        {
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