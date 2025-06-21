using Microsoft.UI.Xaml;

namespace LCTWorks.Common.WinUI.Extensions
{
    public static class AppResourceStringExtensions
    {
        public static T? GetAppResource<T>(this string key)
        {
            return Application.Current.Resources.ContainsKey(key)
                ? (T)Application.Current.Resources[key]
                : default;
        }
    }
}