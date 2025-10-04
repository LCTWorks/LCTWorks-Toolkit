using System;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Animation;
using Windows.UI;

namespace LCTWorks.WinUI.Extensions;

public static class TextBlockExtensions
{
    public static void ToAnimatedForeground(this TextBlock textBlock, Color toColor, TimeSpan duration, bool autoReverse = true)
    {
        // Ensure we animate a local brush (not a shared resource brush)
        var currentColor = (textBlock.Foreground as SolidColorBrush)?.Color ?? Colors.Transparent;
        var animBrush = new SolidColorBrush(currentColor);
        textBlock.Foreground = animBrush;

        var storyboard = new Storyboard();
        var anim = new ColorAnimation
        {
            To = toColor,
            Duration = new Duration(duration),
            AutoReverse = autoReverse,
            EasingFunction = new ExponentialEase { EasingMode = EasingMode.EaseOut }
        };

        Storyboard.SetTarget(anim, animBrush);
        Storyboard.SetTargetProperty(anim, "Color");
        storyboard.Children.Add(anim);
        storyboard.Begin();
    }
}