using LCTWorks.WinUI.Extensions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System;

namespace LCTWorks.WinUI.Helpers;

public static class InAppNotificationHelper
{
    public const int DefaultTime = 5;
    private static readonly DispatcherTimer _timer;

    static InAppNotificationHelper()
    {
        _timer = new DispatcherTimer();
        _timer.Tick += TimerTick;
    }

    public static InfoBar? InfoBar
    {
        get;
        set;
    }

    public static void ShowMessage(string message, NotificationGlyphs glyph = NotificationGlyphs.None, bool closable = true, int seconds = DefaultTime)
    {
        if (InfoBar == null)
        {
            return;
        }

        if (seconds == 0)
        {
            closable = true;
        }
        InfoBar.Message = message;
        InfoBar.Severity = InfoBarSeverity.Informational;
        InfoBar.IsOpen = true;
        InfoBar.IsClosable = closable;
        InfoBar.IsIconVisible = glyph != NotificationGlyphs.None;
        InfoBar.IconSource = GetIconSource(glyph);

        if (_timer.IsEnabled)
        {
            _timer.Stop();
        }

        if (seconds > 0)
        {
            _timer.Interval = TimeSpan.FromSeconds(seconds);
            _timer.Start();
        }
    }

    private static FontIconSource? GetIconSource(NotificationGlyphs glyph)
    {
        if (glyph == NotificationGlyphs.None)
        {
            return null;
        }
        string text = glyph switch
        {
            NotificationGlyphs.Info => "\uE946",
            NotificationGlyphs.Warning => "\uE7BA",
            NotificationGlyphs.Error => "\uEA39",
            _ => ""
        };
        return new FontIconSource
        {
            Glyph = text,
            FontFamily = "FluentIconsFontFamily".GetAppResource<FontFamily>(),
        };
    }

    private static void TimerTick(object? sender, object e)
    {
        if (InfoBar != null)
        {
            InfoBar.IsOpen = false;
        }
        _timer.Stop();
    }
}

public enum NotificationGlyphs
{
    None,
    Info,
    Warning,
    Error,
}