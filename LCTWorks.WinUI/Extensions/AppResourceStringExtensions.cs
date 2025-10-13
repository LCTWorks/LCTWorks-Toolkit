using Microsoft.UI.Xaml;
using System.Collections;
using System.Collections.Generic;

namespace LCTWorks.WinUI.Extensions;

public static class AppResourceStringExtensions
{
    public static T? GetAppResource<T>(this string key)
    {
        var resources = Application.Current?.Resources;
        if (resources is null)
        {
            return default;
        }

        if (TryFindResource(resources, key, out var value) && value is T typed)
        {
            return typed;
        }

        return default;
    }

    private static bool TryFindResource(ResourceDictionary dict, object key, out object? value)
    {
        if (dict.TryGetValue(key, out var found))
        {
            value = found;
            return true;
        }

        foreach (var merged in dict.MergedDictionaries)
        {
            if (TryFindResource(merged, key, out value))
            {
                return true;
            }
        }

        var themeKey = Application.Current?.RequestedTheme == ApplicationTheme.Dark ? "Dark" : "Light";
        if (TryGetThemeDictionary(dict.ThemeDictionaries, themeKey, out var themeDict) &&
            TryFindResource(themeDict, key, out value))
        {
            return true;
        }

        if (TryGetThemeDictionary(dict.ThemeDictionaries, "Default", out var defaultThemeDict) &&
            TryFindResource(defaultThemeDict, key, out value))
        {
            return true;
        }

        value = null;
        return false;

        static bool TryGetThemeDictionary(IDictionary<object, object> themeDictionary, string theme, out ResourceDictionary themeDict)
        {
            themeDict = null!;
            if (themeDictionary.ContainsKey(theme))
            {
                if (themeDictionary[theme] is ResourceDictionary rd)
                {
                    themeDict = rd;
                    return true;
                }
            }
            return false;
        }
    }
}