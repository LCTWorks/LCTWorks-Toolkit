using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.UI.Xaml.Controls;
using System;
using System.Collections.Generic;
using System.Linq;

namespace LCTWorks.WinUI.Navigation
{
    public static class NavigationPageMap
    {
        private static readonly Dictionary<string, Type> _pages = [];

        public static void Configure<VM, V>()
            where VM : ObservableObject
            where V : Page
        {
            lock (_pages)
            {
                var key = typeof(VM).FullName!;
                var type = typeof(V);
                Configure(key, type);
            }
        }

        public static void Configure(string key, Type target)
        {
            lock (_pages)
            {
                if (_pages.ContainsKey(key))
                {
                    throw new ArgumentException($"The key {key} is already configured in PageService");
                }

                if (_pages.ContainsValue(target))
                {
                    throw new ArgumentException($"This type is already configured with key {_pages.First(p => p.Value == target).Key}");
                }

                _pages.Add(key, target);
            }
        }

        public static Type GetPageType(string key)
        {
            Type? pageType;
            lock (_pages)
            {
                if (!_pages.TryGetValue(key, out pageType))
                {
                    throw new ArgumentException($"Page not found: {key}. Did you forget to call PageService.Configure?");
                }
            }

            return pageType;
        }
    }
}