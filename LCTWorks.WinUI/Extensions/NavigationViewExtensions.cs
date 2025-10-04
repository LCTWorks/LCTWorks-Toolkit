using System;
using System.Collections.Generic;
using System.Linq;
using CommunityToolkit.WinUI;
using Microsoft.UI.Xaml.Controls;

namespace LCTWorks.WinUI.Extensions;

public static class NavigationViewExtensions
{
    public static NavigationViewItem? GetNavigationViewItem<T>(this NavigationView nv, Func<T, bool> predicate, bool searchFooter = false)
    {
        var source = searchFooter
            ? nv.FooterMenuItemsSource as IEnumerable<object>
            : nv.MenuItemsSource as IEnumerable<object>;
        if (source == null)
        {
            return default;
        }

        var dataItem = source.OfType<T>().FirstOrDefault(predicate);

        if (dataItem is null)
        {
            return default;
        }

        var container = nv.ContainerFromMenuItem(dataItem) as NavigationViewItem;
        if (container != null)
        {
            return container;
        }

        var repeaterName = searchFooter ? "FooterMenuItemsHost" : "MenuItemsHost";
        var repeater = nv.FindDescendant<ItemsRepeater>(g => g.Name == repeaterName);
        if (repeater is null)
        {
            return default;
        }

        // AdvancedCollectionView is your source type; use IndexOf for the index
        var index = -1;
        if (source is IList<object> acv)
        {
            index = acv.IndexOf(dataItem);
        }
        else
        {
            var i = 0;
            foreach (var it in source)
            {
                if (ReferenceEquals(it, dataItem)) { index = i; break; }
                i++;
            }
        }
        if (index < 0)
        {
            return null;
        }

        // Realize and return the container
        return repeater.TryGetElement(index) as NavigationViewItem;
    }
}