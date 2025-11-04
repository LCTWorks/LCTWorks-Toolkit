using LCTWorks.Core.Helpers;
using LCTWorks.WinUI.Models;
using Windows.Storage;

namespace LCTWorks.WinUI.Helpers;

public static class LocalSettingsHelper
{
    public static string? LastOpenedVersion
    {
        get => ReadSetting<string>(LastOpenedVersionKey);
        set => SaveSetting(LastOpenedVersionKey, value);
    }

    public static string LastOpenedVersionKey
    {
        get;
        set;
    } = "LastOpenedVersion";

    public static RatingPromptData? RatingPromptData
    {
        get => ReadSetting<RatingPromptData>(RatingPromptDataKey);
        set => SaveSetting(RatingPromptDataKey, value);
    }

    public static string RatingPromptDataKey
    {
        get;
        set;
    } = "RatingPromptData";

    public static string? ThemeName
    {
        get => ReadSetting<string?>(ThemeNameKey);
        set => SaveSetting(ThemeNameKey, value);
    }

    public static string ThemeNameKey
    {
        get;
        set;
    } = "AppBackgroundRequestedTheme";

    public static T? ReadSetting<T>(string key, T? defaultValue = default)
    {
        if (RuntimePackageHelper.IsMSIX && ApplicationData.Current.LocalSettings.Values.TryGetValue(key, out var obj))
        {
            return Json.ToObject<T>((string)obj);
        }

        return defaultValue ?? default;
    }

    public static void SaveSetting<T>(string key, T? value)
    {
        if (RuntimePackageHelper.IsMSIX)
        {
            ApplicationData.Current.LocalSettings.Values[key] = Json.Stringify(value);
        }
    }
}