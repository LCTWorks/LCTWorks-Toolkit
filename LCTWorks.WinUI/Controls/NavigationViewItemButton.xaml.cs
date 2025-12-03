using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Controls;

public sealed partial class NavigationViewItemButton : Button
{
    public static readonly DependencyProperty IconProperty =
        DependencyProperty.Register(nameof(Icon), typeof(object), typeof(NavigationViewItemButton),
            new PropertyMetadata(null, OnIconChanged));

    public static readonly DependencyProperty CustomContentProperty =
        DependencyProperty.Register(nameof(CustomContent), typeof(object), typeof(NavigationViewItemButton),
            new PropertyMetadata(null, OnCustomContentChanged));

    public static readonly DependencyProperty IconOnlyProperty =
        DependencyProperty.Register(nameof(IconOnly), typeof(bool), typeof(NavigationViewItemButton),
            new PropertyMetadata(false, OnIconOnlyChanged));

    public object Icon
    {
        get => (object)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public object CustomContent
    {
        get => (object)GetValue(CustomContentProperty);
        set => SetValue(CustomContentProperty, value);
    }

    public bool IconOnly
    {
        get => (bool)GetValue(IconOnlyProperty);
        set => SetValue(IconOnlyProperty, value);
    }

    public NavigationViewItemButton()
    {
        DefaultStyleKey = typeof(NavigationViewItemButton);
    }

    protected override void OnApplyTemplate()
    {
        base.OnApplyTemplate();
        UpdateVisualStates(useTransitions: false);
    }

    private static void OnIconChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NavigationViewItemButton)d;
        control.UpdateVisualStates(useTransitions: true);
    }

    private static void OnCustomContentChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NavigationViewItemButton)d;
        control.UpdateVisualStates(useTransitions: true);
    }

    private static void OnIconOnlyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var control = (NavigationViewItemButton)d;
        control.UpdateVisualStates(useTransitions: true);
    }

    private void UpdateVisualStates(bool useTransitions)
    {
        if (Icon is null)
        {
            VisualStateManager.GoToState(this, "IconHidden", useTransitions);
        }
        else
        {
            VisualStateManager.GoToState(this, IconOnly ? "IconOnly" : "IconVisible", useTransitions);
        }

        var customContentVisible = !IconOnly && CustomContent is not null;
        VisualStateManager.GoToState(this, customContentVisible ? "CustomContentVisible" : "CustomContentHidden", useTransitions);
    }
}