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

    public static bool GetCopyOnFocus(TextBlock textBlock) =>
        (bool)textBlock.GetValue(CopyOnFocusProperty);

    public static void SetCopyOnFocus(TextBlock textBlock, bool value) =>
        textBlock.SetValue(CopyOnFocusProperty, value);

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
}