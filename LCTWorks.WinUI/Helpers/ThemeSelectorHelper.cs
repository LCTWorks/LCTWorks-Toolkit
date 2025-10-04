using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;
using LCTWorks.WinUI.Extensions;

namespace LCTWorks.WinUI.Helpers;

public static class ThemeSelectorHelper
{
    public static ElementTheme Theme { get; set; } = ElementTheme.Default;

    public static void Initialize()
    {
        Theme = LoadThemeFromSettings();
    }

    public static async Task SetRequestedThemeAsync()
    {
        var mainWindows = Application.Current.GetMainWindow();
        if (mainWindows?.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
        }

        await Task.CompletedTask;
    }

    public static async Task SetThemeAsync(ElementTheme theme)
    {
        Theme = theme;

        await SetRequestedThemeAsync();
        SaveThemeInSettings(Theme);
    }

    private static ElementTheme LoadThemeFromSettings()
    {
        var themeName = LocalSettingsHelper.ThemeName;

        if (Enum.TryParse(themeName, out ElementTheme cacheTheme))
        {
            return cacheTheme;
        }

        return ElementTheme.Default;
    }

    private static void SaveThemeInSettings(ElementTheme theme)
        => LocalSettingsHelper.ThemeName = theme.ToString();
}