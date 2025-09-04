using LCTWorks.Common.WinUI.Abstractions;
using LCTWorks.Common.WinUI.Helpers;
using Microsoft.UI.Xaml;
using System;
using System.Threading.Tasks;

namespace LCTWorks.Common.WinUI.Services;

public class ThemeSelectorService
{
    public ElementTheme Theme { get; set; } = ElementTheme.Default;

    public async Task InitializeAsync()
    {
        Theme = LoadThemeFromSettings();
        await Task.CompletedTask;
    }

    public async Task SetRequestedThemeAsync()
    {
        if (Application.Current is IAppExtended appExtended && appExtended.MainWindow.Content is FrameworkElement rootElement)
        {
            rootElement.RequestedTheme = Theme;

            TitleBarHelper.UpdateTitleBar(Theme);
        }

        await Task.CompletedTask;
    }

    public async Task SetThemeAsync(ElementTheme theme)
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