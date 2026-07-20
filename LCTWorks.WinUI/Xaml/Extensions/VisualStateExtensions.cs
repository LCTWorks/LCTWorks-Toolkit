using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Xaml.Extensions;

public static class VisualStateExtensions
{
    public static readonly DependencyProperty ForceVisualStateProperty =
       DependencyProperty.RegisterAttached(
           "ForceVisualState", typeof(string), typeof(VisualStateExtensions),
           new PropertyMetadata(null, OnForceVisualStateChanged));

    public static string GetForceVisualState(Control c) => (string)c.GetValue(ForceVisualStateProperty);

    public static void SetForceVisualState(Control c, string v) => c.SetValue(ForceVisualStateProperty, v);

    private static void OnForceVisualStateChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is not Control control || e.NewValue is not string state || string.IsNullOrEmpty(state))
        {
            return;
        }

        void Apply() => VisualStateManager.GoToState(control, state, false);

        if (control.IsLoaded)
        {
            Apply();
        }
        else
        {
            control.Loaded += (_, _) => Apply();
        }
    }
}