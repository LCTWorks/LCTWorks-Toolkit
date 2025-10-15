using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Extensions;

public static class FrameExtensions
{
    public static object? GetPageProperty(this Frame frame, string propertyName)
        => frame?.Content?
        .GetType()
        .GetProperty(propertyName)
        ?.GetValue(frame.Content, null);
}