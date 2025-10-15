using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;

namespace LCTWorks.WinUI.Extensions;

public static class UIExtensions
{
    public static T? FindControl<T>(this UIElement parent, string ControlName) where T : FrameworkElement
    {
        if (parent == null)
        {
            return null;
        }

        if (parent.GetType() == typeof(T) && ((T)parent).Name == ControlName)
        {
            return (T)parent;
        }
        T? result = null;
        var count = VisualTreeHelper.GetChildrenCount(parent);
        for (var i = 0; i < count; i++)
        {
            var child = (UIElement)VisualTreeHelper.GetChild(parent, i);

            if (FindControl<T>(child, ControlName) != null)
            {
                result = FindControl<T>(child, ControlName);
                break;
            }
        }
        return result;
    }
}